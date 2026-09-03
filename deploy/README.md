# Server deployment runbook

Deploys the stack onto a single Docker host from images published to GHCR by CI.
Written for the Hetzner CPX22 (2 vCPU / 4 GB) test box.

**Scope:** this is a test deployment. The API is served over **plain HTTP** with no
reverse proxy, so credentials and JWTs cross the network in clear text. Do not put
real tenant data behind it until TLS is in front (see [Adding TLS later](#adding-tls-later)).

---

## 1. Prepare the host

The server was created without an SSH key, so log in with the root password Hetzner
emailed you (or use the browser console in the Hetzner dashboard):

```bash
ssh root@<server-ip>
```

### Harden the login first

Root SSH with a password is scanned and brute-forced continuously — usually within
minutes of a new IP going live. Before anything else, switch to key auth. From your
**laptop**:

```bash
ssh-keygen -t ed25519 -C "tmg-hetzner"        # if you have no key yet
ssh-copy-id root@<server-ip>                  # prompts for the root password once
```

Confirm `ssh root@<server-ip>` now logs in without asking for a password, then on the
**server** turn password auth off:

```bash
sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication no/' /etc/ssh/sshd_config
systemctl restart ssh
```

Keep your current session open while you test a second login — if key auth is broken
you would otherwise lock yourself out (Hetzner's browser console is the way back in).

If you would rather not do this yet, at minimum install `fail2ban`:
`apt update && apt install -y fail2ban`

### Install Docker

If you did not pick Hetzner's "Docker CE" app image, install Docker:

```bash
curl -fsSL https://get.docker.com | sh
```

Verify: `docker --version && docker compose version`

### Firewall

Only SSH and HTTP should be reachable. The compose file binds Postgres, Redis and
the RabbitMQ UI to `127.0.0.1`, but a firewall is the backstop:

```bash
ufw allow 22/tcp
ufw allow 80/tcp
ufw --force enable
```

A Hetzner Cloud Firewall in the console does the same job outside the VM and is
worth adding as well.

---

## 2. Get the code

The compose file reads the OTel collector config from `../observability/`, so clone
the whole repo rather than copying one file:

```bash
git clone https://github.com/kaobinzeh/tmg-backend.git
cd tmg-backend/deploy
```

---

## 3. GHCR access — nothing to do

The four packages are public, confirmed by an anonymous pull against the registry,
so the server needs no registry credentials.

If that ever changes (packages default to private, and visibility can be reset),
`docker compose pull` will fail with `denied`. Fix it either by setting each package
back to Public in GitHub → Package settings → Change visibility, or by logging in on
the server with a classic PAT carrying `read:packages`:

```bash
echo "<your-pat>" | docker login ghcr.io -u kaobinzeh --password-stdin
```

---

## 4. Configure secrets

```bash
cp .env.example .env
```

Generate real values — **do not reuse the committed placeholders**:

```bash
openssl rand -base64 24   # POSTGRES_PASSWORD
openssl rand -base64 24   # RABBITMQ_PASSWORD
openssl rand -base64 48   # JWT_SIGNING_KEY
```

Then edit `.env` and fill in every blank, including the Grafana Cloud values
(the collector exits at startup if `GRAFANA_CLOUD_OTLP_ENDPOINT` is empty).

```bash
nano .env
chmod 600 .env
```

---

## 5. Start it

```bash
docker compose pull
docker compose up -d
```

First start runs the migrator against an empty database before the three app
services come up, so expect 30–60 seconds before everything is healthy.

---

## 6. Verify

```bash
docker compose ps                    # all services Up, apps (healthy)
curl -i http://localhost/health      # 200
curl -s http://localhost/metrics | head
docker compose logs otel-collector | grep -i error   # expect nothing
```

From your laptop: `curl -i http://<server-ip>/health`

Telemetry should appear in Grafana Cloud within a minute under service names
`TMG.WebAPI`, `TMG.Consumer`, `TMG.Jobs`.

---

## 7. Deploying a new version

CI publishes `:latest` and `:<sha>` on every push to `master`:

```bash
cd ~/tmg-backend && git pull
cd deploy
docker compose pull
docker compose up -d
docker image prune -f
```

To pin an exact build instead of tracking `latest`, set `IMAGE_TAG=<commit-sha>`
in `.env` before `docker compose pull`.

---

## Administering the datastores

Nothing but the API is exposed publicly. Tunnel from your laptop:

```bash
ssh -L 5432:localhost:5432 root@<server-ip>     # then psql to localhost:5432
ssh -L 15672:localhost:15672 root@<server-ip>   # then open http://localhost:15672
```

---

## Backups

Data lives in the `tmg_postgres_data` volume, which survives `docker compose down`
but not a destroyed server. There is **no automatic backup**. A minimal daily dump:

```bash
mkdir -p /root/backups
cat >/etc/cron.daily/tmg-db-backup <<'EOF'
#!/bin/sh
cd /root/tmg-backend/deploy
docker compose exec -T postgres pg_dump -U tmg TMGDb \
  | gzip > /root/backups/tmg-$(date +%F).sql.gz
find /root/backups -name 'tmg-*.sql.gz' -mtime +7 -delete
EOF
chmod +x /etc/cron.daily/tmg-db-backup
```

That keeps a week of dumps on the same disk — it protects against a bad migration,
not against losing the server. Copy them off-box (Hetzner Storage Box, Cloudflare R2)
before this holds anything that matters.

---

## Adding TLS later

Add a Caddy service, change the `webapi` port mapping from `80:8080` to nothing
(Caddy reaches it in-network), and set `ForwardedHeaders__Enabled: "true"` so
per-IP rate limiting sees the real client address instead of Caddy's.
With a domain pointed at the box, Caddy issues a Let's Encrypt certificate on its
own; `<ip-with-dashes>.sslip.io` works without any DNS setup.

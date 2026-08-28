# Backend Project Template

A .NET 10 backend starter organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, a transactional outbox with RabbitMQ dispatching, OpenTelemetry, and containerized development dependencies.

## Included

- `src/TMG.Domain`: entities, repository/specification abstractions, and infrastructure-facing interfaces
- `src/TMG.Application`: vertical slices for use cases, DTOs, handlers, and specifications
- `src/TMG.Infrastructure`: EF Core, Redis cache, JWT, OTP delivery, telemetry, and other implementations
- `src/TMG.DatabaseMigrator`: dedicated deployment-time migrator that runs pre-deploy SQL, EF Core migrations, seed data, and post-deploy SQL
- `src/TMG.WebAPI`: controller-based HTTP host and presentation layer
- `src/TMG.Consumer`: worker placeholder for async message consumption with readiness and liveness endpoints
- `src/TMG.Jobs`: worker placeholder for scheduled work and transactional outbox dispatching with readiness and liveness endpoints
- `tests/unit/TMG.Application.UnitTests`: unit tests for application handlers and use cases
- `tests/unit/TMG.Domain.UnitTests`: unit tests for domain entities and behavior
- `tests/unit/TMG.Infrastructure.UnitTests`: unit tests for infrastructure components
- `tests/unit/TMG.WebAPI.UnitTests`: unit tests for WebAPI-specific helpers
- `tests/unit/TMG.Consumer.UnitTests`: unit tests for consumer worker behavior
- `tests/unit/TMG.Jobs.UnitTests`: unit tests for scheduled jobs worker behavior
- `tests/integration/TMG.WebAPI.IntegrationTests`: WebAPI integration tests using SQL Server and Redis testcontainers
- `tests/integration/TMG.Consumer.IntegrationTests`: consumer host integration tests using SQL Server and Redis testcontainers
- `tests/integration/TMG.Jobs.IntegrationTests`: jobs host integration tests using SQL Server and Redis testcontainers

## Architecture Notes

- Domain owns entities plus contracts such as repositories, cache interfaces, token generation, and OTP delivery
- Application keeps vertical slices by feature and depends only on the domain
- Infrastructure contains EF Core persistence, Redis caching, JWT generation, observability, and other implementation details
- Asynchronous integration messages are persisted through a transactional outbox and dispatched from the Jobs service through RabbitMQ
- Database changes are applied by a separate migrator service before the other services are deployed
- The migrator exposes readiness and liveness endpoints so deployment orchestration can distinguish between startup and completed database work
- WebAPI is only the presentation host and endpoint mapping layer
- Schemas are separated by domain using `authentication` and `reference_data`
- `TimeProvider` is the standard time abstraction used across handlers and infrastructure

## Template Usage

Install the template from the repository root:

```powershell
dotnet new install .
```

Create a new solution:

```powershell
dotnet new backend-template --organizationAbbreviation CN --clientName Acme --clientProjectName Ordering -o .\CN.Acme.Ordering
```

This creates a fresh project tree without the template repository's `.git` history.

The generated root name becomes `{OrganizationAbbreviation}.{ClientName}.{ClientProjectName}` and is applied to the solution, projects, folders, and namespaces. The organization abbreviation is intended for short forms such as `CN` and should be at most 3 characters.

If you want an interactive prompt instead of typing the parameters yourself, run:

```powershell
.\scripts\New-BackendProject.ps1
```

The script prompts for organization abbreviation, client name, and client project name, then installs the local template and creates the solution for you. If you leave organization blank, it defaults to `CN`.
When `git` is available on `PATH`, the script also initializes a new repository in the generated project directory.

There is also a bash version:

```bash
./scripts/New-BackendProject.sh
```

It supports the same inputs and generated naming convention.
When `git` is available on `PATH`, it also initializes a new repository in the generated project directory.

## Local Development

Restore, build, and test:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
dotnet restore
dotnet build
dotnet test
```

Run the database migrator on its own:

```powershell
dotnet run --project src/TMG.DatabaseMigrator
```

The migrator executes scripts in:

- `src/TMG.DatabaseMigrator/Scripts/PreDeploy`
- `src/TMG.DatabaseMigrator/Scripts/PostDeploy`

In `docker compose`, the migrator stays running and only becomes healthy after the database work completes. The other services depend on that liveness state and will not start while the migrator is still unhealthy or has failed.

The migrator health endpoints are:

- Readiness: `http://localhost:8080/health/readiness`
- Liveness: `http://localhost:8080/health/liveness`

`/health/readiness` returns healthy while the migrator is available to execute the deployment work. `/health/liveness` only returns healthy after pre-deploy SQL, EF migrations, seed data, and post-deploy SQL have all completed successfully.

Start the local stack:

```powershell
docker compose up --build
```

Useful endpoints:

- API: `http://localhost:8080`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- Metrics: `http://localhost:8080/metrics`
- Health: `http://localhost:8080/health`
- RabbitMQ: `amqp://localhost:5672`
- RabbitMQ Management: `http://localhost:15672`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Tempo: `http://localhost:3200`
- Pyroscope: `http://localhost:4040`

The `consumer` and `jobs` containers expose internal `/health/readiness` and `/health/liveness` endpoints for orchestration. In `docker compose`, both services wait for the database migrator to complete successfully before starting.

## Sandbox and Staging Deployment

Both run with `ASPNETCORE_ENVIRONMENT=Container`, which layers `appsettings.Container.json` over
`appsettings.json`. Deployment-specific values are supplied as environment variables so nothing
environment-specific is committed.

### Reverse proxy headers

The client IP partitions every rate-limit policy and is recorded against sessions. Behind an ingress
the connection address is the proxy's, so without forwarded-header processing every caller shares one
partition and the anonymous rate limit throttles all traffic collectively.
`appsettings.Container.json` enables it.

| Variable | Container default | Notes |
|---|---|---|
| `ForwardedHeaders__Enabled` | `true` | Disable only when nothing fronts the service. |
| `ForwardedHeaders__TrustAnyProxy` | `true` | Accepts `X-Forwarded-For` from any peer. Safe only while the ingress is the sole route in. |
| `ForwardedHeaders__ForwardLimit` | `1` | Proxy hops to unwind, counted from the right of the header. |
| `ForwardedHeaders__KnownProxies__0` | unset | Pin individual ingress addresses instead of `TrustAnyProxy`. |
| `ForwardedHeaders__KnownNetworks__0` | unset | Same, in CIDR form such as `10.0.0.0/8`. |

Startup fails when the section is enabled while no proxy is trusted, because the middleware would
otherwise ignore every forwarded header without saying so.

### Frontend origins

CORS is deny-by-default and the allowlist is empty in committed configuration. Set one variable per
origin:

    Cors__AllowedOrigins__0=https://staging.app.example
    Cors__AllowedOrigins__1=https://preview.app.example

An empty allowlist is a valid server-to-server deployment, so it logs a warning at startup rather
than failing — an unset variable and a deliberate empty list look identical otherwise.

### Client onboarding

`Clients:Onboarding:DefaultClientId` in `appsettings.json` holds the default client the post-deploy
seeder creates. Sign-up attaches new stakeholders to it, and startup fails when the value is missing
so a mis-seeded database is caught at deploy time instead of on the first registration. Override with
`Clients__Onboarding__DefaultClientId` when a deployment seeds a different client.

## Profiling

The local observability stack includes Grafana Pyroscope for continuous profiling.

- Grafana provisions a `Pyroscope` data source automatically.
- The `webapi`, `consumer`, and `jobs` containers are built with the native .NET Pyroscope profiler and push profiles directly to `http://pyroscope:4040`.
- The current profiling setup is container-only. Grafana's .NET profiler currently supports Linux on `amd64`, so `dotnet run` on Windows/macOS is not profiled by this configuration.

After `docker compose up --build`, open Grafana at `http://localhost:3000` and use Profiles Drilldown or Explore with the `Pyroscope` data source to inspect:

- `tmg.webapi`
- `tmg.consumer`
- `tmg.jobs`

Default SQL Server credentials in the template:

- user: `sa`
- password: `Your_strong_Password123!`

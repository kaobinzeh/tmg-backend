#!/usr/bin/env bash

export GITHUB_USERNAME=""
export GITHUB_PAT=""
export MAILTRAP_TOKEN=""

# Grafana Cloud OTLP export (used by the otel-collector service).
# Values come from Grafana Cloud → your stack → OpenTelemetry → "Configure".
export GRAFANA_CLOUD_OTLP_ENDPOINT=""   # e.g. https://otlp-gateway-prod-eu-west-2.grafana.net/otlp
export GRAFANA_CLOUD_INSTANCE_ID=""     # numeric stack id
export GRAFANA_CLOUD_API_TOKEN=""       # Cloud Access Policy token: metrics:write, logs:write, traces:write

# Tenancy Management Core Backend

A .NET 10 tenancy management core backend organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, a transactional outbox with RabbitMQ dispatching, OpenTelemetry, and containerized development dependencies.

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

## Profiling

The local observability stack includes Grafana Pyroscope for continuous profiling.

- Grafana provisions a `Pyroscope` data source automatically.
- The `webapi`, `consumer`, and `jobs` containers are built with the native .NET Pyroscope profiler and push profiles directly to `http://pyroscope:4040`.
- The current profiling setup is container-only. Grafana's .NET profiler currently supports Linux on `amd64`, so `dotnet run` on Windows/macOS is not profiled by this configuration.

After `docker compose up --build`, open Grafana at `http://localhost:3000` and use Profiles Drilldown or Explore with the `Pyroscope` data source to inspect:

- `tmg.webapi`
- `tmg.consumer`
- `tmg.jobs`

Default database credentials:

- user: `sa`
- password: `Your_strong_Password123!`

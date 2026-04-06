# WiSave Expenses Microservice

Event-sourced expenses backend with DDD, CQRS, and MassTransit.

## Architecture

- **KurrentDB** — event store (source of truth)
- **PostgreSQL** — config/reference data + read model projections
- **RabbitMQ/MassTransit** — async command transport and committed-event distribution

### Executables

| Service | Description | Port |
|---------|-------------|------|
| `WebApi` | REST API — publishes commands, queries projections | 5200 |
| `Worker.Domain` | Consumes commands, executes domain logic, appends events, forwards committed KurrentDB events to RabbitMQ | — |
| `Worker.Projections` | Consumes RabbitMQ committed events and builds read models in Postgres | — |

### Class Libraries

| Library | Responsibility |
|---------|---------------|
| `Contracts` | Shared commands, integration events, models (NuGet package) |
| `Core.Domain` | Aggregates, domain events, value objects (zero dependencies) |
| `Core.Application` | Command handlers, repository abstractions |
| `Core.Infrastructure` | KurrentDB, Postgres, MassTransit implementations |
| `Projections` | Read models, projectors, query handlers |

## Runtime Flow

1. `WebApi` publishes commands to RabbitMQ.
2. `Worker.Domain` consumes commands and appends domain events to KurrentDB.
3. `Worker.Domain` forwarder reads committed events from a Kurrent persistent subscription and republishes them to RabbitMQ.
   The subscription group is configured for a single active subscriber using `DispatchToSingle`.
4. `Worker.Projections` consumes committed events from RabbitMQ and updates Postgres read models.
5. `WebApi` reads projections from Postgres.

## Local Development

```bash
# Build
dotnet build WiSave.Expenses.slnx

# Run tests
dotnet test WiSave.Expenses.slnx

# Run with Docker
docker compose up --build
```

### Ports

| Service | Port |
|---------|------|
| Expenses WebApi | 5200 |
| KurrentDB UI | 2113 |
| PostgreSQL | 5433 |

RabbitMQ is hosted by Portal (`localhost:5672`).

## Contracts Package Versioning

`WiSave.Expenses.Contracts` uses `Nerdbank.GitVersioning` for package versions.

- Pull requests targeting `master` publish preview packages to GitHub Packages.
- Pushes to `master` publish stable packages to the same feed.
- The package version is not hardcoded in the contracts `.csproj`; it is derived from git history and `version.json`.

The current contracts version line starts at `1.0`.

## Design Docs

- [Microservice Design Spec](../wisave-documentation/specs/2026-03-28-expenses-microservice-design.md)
- [Implementation Plan](../wisave-documentation/plans/2026-03-28-expenses-microservice-implementation.md)
- [API Contract ADR](../wisave-documentation/adr/2026-03-28-expenses-api-contract-alignment.md)

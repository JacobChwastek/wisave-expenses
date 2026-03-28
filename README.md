# WiSave Expenses Microservice

Event-sourced expenses backend with DDD, CQRS, and MassTransit.

## Architecture

- **KurrentDB** — event store (source of truth)
- **PostgreSQL** — config/reference data + read model projections
- **RabbitMQ/MassTransit** — async commands and integration events (hosted by Portal)

### Executables

| Service | Description | Port |
|---------|-------------|------|
| `WebApi` | REST API — publishes commands, queries projections | 5200 |
| `Worker.Domain` | Consumes commands, executes domain logic, appends events | — |
| `Worker.Projections` | KurrentDB subscriptions, builds read models in Postgres | — |

### Class Libraries

| Library | Responsibility |
|---------|---------------|
| `Contracts` | Shared commands, integration events, models (NuGet package) |
| `Core.Domain` | Aggregates, domain events, value objects (zero dependencies) |
| `Core.Application` | Command handlers, repository abstractions |
| `Core.Infrastructure` | KurrentDB, Postgres, MassTransit implementations |
| `Subscriptions` | MassTransit consumer definitions |
| `Projections` | Read models, projectors, query handlers |

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

## Design Docs

- [Microservice Design Spec](../wisave-documentation/specs/2026-03-28-expenses-microservice-design.md)
- [Implementation Plan](../wisave-documentation/plans/2026-03-28-expenses-microservice-implementation.md)
- [API Contract ADR](../wisave-documentation/adr/2026-03-28-expenses-api-contract-alignment.md)

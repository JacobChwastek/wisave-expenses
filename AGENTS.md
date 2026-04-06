# AGENTS.md

Guidance for coding agents working in this repository.

## Project Overview

- `WiSave.Expenses` is an event-sourced expenses microservice using DDD and CQRS.
- Domain events are stored in KurrentDB, and read models are projected into PostgreSQL.
- MassTransit is used for command and integration message flow with other services.
- Keep changes focused, preserve existing architecture boundaries, and avoid broad refactors.

## Repository Layout

- `src/WiSave.Expenses.WebApi` — REST endpoints and authorization checks
- `src/WiSave.Expenses.Worker.Domain` — command consumers and domain execution pipeline
- `src/WiSave.Expenses.Worker.Projections` — projection workers and subscription runtime
- `src/WiSave.Expenses.Core.Domain` — aggregates, domain events, value objects
- `src/WiSave.Expenses.Core.Application` — command handlers, application workflows, abstractions
- `src/WiSave.Expenses.Core.Infrastructure` — KurrentDB, Postgres, messaging, identity integration
- `src/WiSave.Expenses.Projections` — read models, projectors, repositories, query handlers
- `src/WiSave.Expenses.Contracts` — shared contracts and event/message models
- `src/WiSave.Expenses.Console` — local operational/administrative console commands
- `src/WiSave.Expenses.Core.Migrations` — core schema migration support
- `src/WiSave.Expenses.Projections.Migrations` — projection schema migration support
- `tests/*` — unit and integration tests split by project responsibility

## Working Style

- Prefer minimal changes that solve the root cause.
- Follow existing naming, layering, and dependency direction.
- Do not mix unrelated cleanup into feature or bugfix work.
- Keep contracts backwards compatible unless a breaking change is explicitly requested.
- When behavior changes, update tests in the nearest relevant test project.

## Build, Run, and Test

Use the existing solution commands:

```bash
dotnet build WiSave.Expenses.slnx
dotnet test WiSave.Expenses.slnx
docker compose up --build
dotnet run --project src/WiSave.Expenses.WebApi
dotnet run --project src/WiSave.Expenses.Worker.Domain
dotnet run --project src/WiSave.Expenses.Worker.Projections
```

## Testing Expectations

- Run targeted tests first for changed areas, then broader validation.
- Do not claim completion without running relevant verification commands.
- If verification cannot be run, state that clearly and list what should be executed.
- Favor tests that validate event flow correctness and projection outcomes.

## Expenses-Specific Guidance

- Preserve event-sourcing invariants: domain state must be derived from events.
- Keep command handling deterministic and idempotent where retries are possible.
- Ensure projections tolerate eventual consistency and replay behavior.
- Keep API and contracts aligned with current Portal integration assumptions.
- Changes to event contracts require careful compatibility consideration for existing consumers.

## Migrations and Data Changes

- Use the appropriate migrations projects for schema changes.
- Keep runtime persistence changes aligned with migration scripts and tests.
- Mention migration or replay requirements in handoff notes.
- Treat EF Core migrations as the source of truth for schema changes.
- Keep DbUp responsible for per-schema bootstrap only: creating the target schema if needed and maintaining a schema-scoped `SchemaVersions` journal.
- Recommended schema-change flow:
  1. Apply code/model changes in EF Core.
  2. Generate the EF Core migration.
  3. Generate SQL from that EF migration.
  4. Copy the generated SQL into the next DbUp script in the matching migrations project.
- Do not have the agent invent, regenerate, or hand-author DbUp payload SQL on its own.
- The EF-migration-to-DbUp-script step is a manual developer workflow and should be called out to the developer instead of being improvised automatically in-session.
- Do not hand-rewrite existing EF-derived DbUp migration payloads unless a change is explicitly requested.
- Avoid shipping placeholder or comment-only DbUp scripts. DbUp will still journal an executed script by filename, so if a no-op script has already been applied, any later real change must go into a new numbered script instead of reusing that filename.
- For local console verification, the expenses Postgres runs on host port `5433` via `docker compose`.

## Wiki Maintenance

This repository has a GitHub Wiki mounted as a Git submodule at:

```text
wiki/
```

Treat `wiki/` as the documentation workspace for GitHub Wiki pages.

When the user asks to analyze an area of the application and improve, refresh, or create wiki pages:

1. Inspect the relevant source code first.
2. Inspect related tests, configuration, README files, existing docs, and existing wiki pages.
3. Identify behavior from code, tests, and configuration instead of assumptions.
4. Create or update Markdown pages inside `wiki/`.
5. Keep pages practical and developer-oriented.
6. Update `Home.md` and `_Sidebar.md` when adding, renaming, or removing important pages.
7. Add relative links between wiki pages where useful.
8. Add source references to relevant repository files using paths relative to the repository root, for example:
   - `src/WiSave.Expenses.Worker.Domain/...`
   - `tests/WiSave.Expenses.Worker.Domain.Tests/...`
9. Do not expose secrets, connection strings, tokens, private credentials, or production-only sensitive values in the wiki.

Before and after modifying wiki files:

- Check wiki status with `git -C wiki status`.
- If `git -C wiki rev-parse --show-toplevel` resolves to the parent repository instead of `wiki/`, the wiki submodule is not initialized; stop and ask the developer to initialize/update the submodule before editing wiki pages.
- Review wiki diff with `git -C wiki diff`.
- Review parent repository status with `git status`.
- Remember that changes inside `wiki/` belong to the wiki repository.
- The parent repository may also show the submodule pointer as changed after commits are made inside the wiki.
- Do not commit either the wiki repository or the parent repository unless explicitly asked.

Before every commit:

- Check whether local agent planning docs exist under ignored `docs/superpowers/`.
- Decide whether any local superpowers docs contain durable architecture, operations, troubleshooting, migration, or business-decision content that belongs in `wiki/`.
- If yes, copy the durable content into the appropriate wiki page first, validate `git -C wiki diff`, and commit/push the wiki repository before committing the parent repository.
- If no, leave `docs/superpowers/` untracked and mention that no wiki promotion was needed.

Use this structure for substantial wiki pages:

```md
# Page Title
## Purpose
## When to use / when it runs
## Main flow
## Key components
## Configuration
## Failure modes and troubleshooting
## Source map
## Open questions
```

## Agent Handoff

When finishing work:

- summarize what changed and why
- list files touched
- note verification performed
- call out follow-up steps, risks, and required run commands

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- After modifying code files in this session, run `python3 -c "from graphify.watch import _rebuild_code; from pathlib import Path; _rebuild_code(Path('.'))"` to keep the graph current

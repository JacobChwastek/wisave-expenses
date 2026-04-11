# KurrentDB Committed Events to RabbitMQ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish only committed domain events from KurrentDB to RabbitMQ, then harden projections so at-least-once delivery does not corrupt read models.

**Architecture:** Keep KurrentDB as source of truth and add a dedicated bridge in `Worker.Domain` that reads committed events from KurrentDB and republishes them to RabbitMQ. Use persisted checkpoints to resume reliably after restarts. Make projection consumers idempotent using a processed-message table and transactionally apply projection updates with dedup marking.

**Tech Stack:** .NET 10, EventStore.Client (KurrentDB), MassTransit + RabbitMQ, EF Core 10 + PostgreSQL, xUnit.

---

## Graphify-Derived Context (Used To Improve This Plan)

- `graphify-out/GRAPH_REPORT.md` identifies the architecture hyperedge `Command Flow: WebApi -> MassTransit -> Worker.Domain -> KurrentDB` and separately `Projection Pipeline: KurrentDB -> Worker.Projections -> PostgreSQL`. In real code, `Worker.Projections` currently consumes from RabbitMQ, not from KurrentDB directly. This plan resolves that mismatch by adding a KurrentDB->RabbitMQ bridge in `Worker.Domain`.
- Graph communities show a tight cluster around `KurrentDbAggregateRepository`, `IAggregateRepository`, and projection handlers (`AccountEventHandler`, `BudgetEventHandler`, `ExpenseEventHandler`), indicating those are the highest-leverage points for reliability fixes.
- `ProjectionCheckpoint` appears as an isolated but available primitive. This plan intentionally reuses it for forwarder checkpoint durability instead of introducing a second checkpoint mechanism.
- Degree hotspots are mostly tests and endpoint classes, which means core runtime coupling is not immediately visible without targeted analysis. Tasks are ordered to first stabilize infrastructure contracts (type registry), then runtime forwarding, then projection correctness.
- Graph extraction confidence for key call links inside `ExpenseEventHandler` and `KurrentDbAggregateRepository` is partly inferred (0.5). This plan keeps implementation guarded with explicit tests to avoid relying on inferred graph edges alone.

## Execution Order Rationale

- Task 1 before Task 2: forwarding requires robust event-type resolution; centralizing this first prevents duplicated mapper logic.
- Task 2 before Task 3/4: once committed events are actually forwarded, projection idempotency and correctness become critical operationally.
- Task 3 before Task 4: idempotency/transactions are foundational safety rails; recalculation logic can then be corrected with lower regression risk.
- Task 5 last: documentation and smoke verification only after behavior is implemented and tested.

---

## File Structure

- `src/WiSave.Expenses.Core.Infrastructure/EventStore/ContractEventTypeRegistry.cs`
Responsibility: shared mapping between event type names in KurrentDB and contract CLR types.

- `src/WiSave.Expenses.Core.Infrastructure/EventStore/KurrentDbAggregateRepository.cs`
Responsibility: aggregate load/save using shared event type registry (no duplicated reflection map).

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentForwarderOptions.cs`
Responsibility: settings for subscription id and start behavior.

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentCheckpointStore.cs`
Responsibility: load/save forwarder checkpoint in PostgreSQL.

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentToRabbitForwarder.cs`
Responsibility: subscribe to KurrentDB, deserialize committed events, publish to RabbitMQ, persist checkpoint.

- `src/WiSave.Expenses.Worker.Domain/Program.cs`
Responsibility: register forwarder hosted service and options.

- `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
Responsibility: forwarder config.

- `src/WiSave.Expenses.Projections/ReadModels/ProcessedMessageReadModel.cs`
Responsibility: dedup table model for consumed messages.

- `src/WiSave.Expenses.Projections/ProjectionsDbContext.cs`
Responsibility: dedup table mapping and uniqueness constraints for idempotent read-model writes.

- `src/WiSave.Expenses.Projections/EventHandlers/AccountEventHandler.cs`
- `src/WiSave.Expenses.Projections/EventHandlers/BudgetEventHandler.cs`
- `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
Responsibility: transactional idempotent handling and projection updates.

- `src/WiSave.Expenses.Projections.Migrations/Scripts/003_ProcessedMessagesAndProjectionConstraints.sql`
Responsibility: schema changes for idempotency and projection consistency.

- `tests/WiSave.Expenses.Worker.Domain.Tests/*`
Responsibility: bridge behavior tests (type resolution, checkpoint updates, publish call, skip unknown event type).

- `tests/WiSave.Expenses.Projections.Tests/*`
Responsibility: projection idempotency and expense-update aggregate-correction tests.

- `WiSave.Expenses.slnx`
Responsibility: include new test project(s).

- `README.md`
Responsibility: document real runtime flow and operations.

---

### Task 1: Introduce Shared Contract Event Type Registry

**Context and Problem:**
- Graph + code both show event type resolution currently buried in `KurrentDbAggregateRepository` as a local map build.
- Forwarder implementation (Task 2) will need the same mapping semantics.
- Duplicating type mapping in two places increases drift risk and silent deserialization mismatches.

**Why this task exists:**
- Establish one canonical event-type resolver for all KurrentDB readers.
- Make the mapping testable in isolation before introducing continuous subscriptions.

**Success criteria:**
- Known contract event names resolve deterministically.
- Repository uses shared registry; no private duplicate type-map logic remains.
- New worker-domain tests compile and pass in isolation.

**Files:**
- Create: `src/WiSave.Expenses.Core.Infrastructure/EventStore/ContractEventTypeRegistry.cs`
- Modify: `src/WiSave.Expenses.Core.Infrastructure/EventStore/KurrentDbAggregateRepository.cs`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/EventStore/ContractEventTypeRegistryTests.cs`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj`
- Modify: `WiSave.Expenses.slnx`

- [ ] **Step 1: Write the failing registry test**

```csharp
using WiSave.Expenses.Core.Infrastructure.EventStore;

namespace WiSave.Expenses.Worker.Domain.Tests.EventStore;

public class ContractEventTypeRegistryTests
{
    [Fact]
    public void Resolve_returns_contract_type_for_known_event_name()
    {
        var registry = new ContractEventTypeRegistry();
        var resolved = registry.Resolve("AccountOpened");

        Assert.NotNull(resolved);
        Assert.Equal("WiSave.Expenses.Contracts.Events.Accounts.AccountOpened", resolved!.FullName);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal`  
Expected: FAIL with missing `ContractEventTypeRegistry`.

- [ ] **Step 3: Implement registry and wire repository to use it**

```csharp
namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public sealed class ContractEventTypeRegistry
{
    private readonly Dictionary<string, Type> _typeByName;

    public ContractEventTypeRegistry()
    {
        var contractsAssembly = typeof(WiSave.Expenses.Contracts.Events.CommandFailed).Assembly;
        _typeByName = contractsAssembly
            .GetExportedTypes()
            .Where(t => t.Namespace?.Contains(".Events.") == true)
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
    }

    public Type? Resolve(string eventTypeName) =>
        _typeByName.TryGetValue(eventTypeName, out var type) ? type : null;
}
```

```csharp
// KurrentDbAggregateRepository constructor
public sealed class KurrentDbAggregateRepository<T>(
    EventStoreClient client,
    ContractEventTypeRegistry eventTypeRegistry) : IAggregateRepository<T>
    where T : AggregateRoot, new()
{
    private Type? ResolveEventType(string eventTypeName) => eventTypeRegistry.Resolve(eventTypeName);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add WiSave.Expenses.slnx \
  src/WiSave.Expenses.Core.Infrastructure/EventStore/ContractEventTypeRegistry.cs \
  src/WiSave.Expenses.Core.Infrastructure/EventStore/KurrentDbAggregateRepository.cs \
  tests/WiSave.Expenses.Worker.Domain.Tests
git commit -m "refactor(eventstore): centralize contract event type resolution"
```

### Task 2: Build KurrentDB to RabbitMQ Forwarder with Checkpointing

**Context and Problem:**
- The dominant architecture gap is absence of committed-event propagation after append.
- Graph hyperedge labels imply KurrentDB-driven projections, but runtime wiring is RabbitMQ-based projection consumers.
- Without a bridge, events are committed but not externally propagated.

**Why this task exists:**
- Introduce a single, durable outbound path that republishes only committed events.
- Preserve event-store-first consistency by publishing from subscription stream, not from pre-commit in-memory state.
- Ensure crash-safe resume via persisted checkpoints.

**Success criteria:**
- Forwarder subscribes from saved checkpoint and republishes recognized events to RabbitMQ.
- Checkpoint advances only after successful publish.
- Unknown/unmapped events are handled explicitly (skip + log), not fatal to subscription loop.

**Files:**
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentForwarderOptions.cs`
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentCheckpointStore.cs`
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentToRabbitForwarder.cs`
- Modify: `src/WiSave.Expenses.Worker.Domain/Program.cs`
- Modify: `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/Forwarding/KurrentToRabbitForwarderTests.cs`

- [ ] **Step 1: Write failing forwarder behavior test**

```csharp
public class KurrentToRabbitForwarderTests
{
    [Fact]
    public async Task HandleEvent_publishes_known_contract_event_and_updates_checkpoint()
    {
        // Arrange test doubles for publish endpoint + checkpoint store
        // Feed one AccountOpened JSON payload with stream position 42
        // Assert publish called once and checkpoint saved with 42
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter KurrentToRabbitForwarderTests`  
Expected: FAIL due to missing forwarder classes.

- [ ] **Step 3: Implement forwarder options, checkpoint store, and hosted service**

```csharp
public sealed class KurrentForwarderOptions
{
    public string CheckpointId { get; set; } = "kurrent-to-rabbit-forwarder";
    public bool StartFromBeginningWhenNoCheckpoint { get; set; } = true;
}
```

```csharp
public sealed class KurrentCheckpointStore(ProjectionsDbContext db)
{
    public async Task<ulong?> LoadAsync(string id, CancellationToken ct)
        => (await db.Checkpoints.FindAsync([id], ct))?.Position;

    public async Task SaveAsync(string id, ulong position, CancellationToken ct)
    {
        var row = await db.Checkpoints.FindAsync([id], ct);
        if (row is null)
            db.Checkpoints.Add(new ProjectionCheckpoint { Id = id, Position = position, UpdatedAt = DateTimeOffset.UtcNow });
        else
        {
            row.Position = position;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}
```

```csharp
public sealed class KurrentToRabbitForwarder : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1) load checkpoint
        // 2) subscribe to KurrentDB from checkpoint
        // 3) resolve event type + deserialize payload
        // 4) publish to RabbitMQ with context.MessageId = resolved.Event.EventId.ToGuid()
        // 5) persist checkpoint only after successful publish
    }
}
```

- [ ] **Step 4: Wire forwarder into Worker.Domain composition root**

```csharp
// Program.cs
builder.Services.Configure<KurrentForwarderOptions>(
    builder.Configuration.GetSection("KurrentForwarder"));
builder.Services.AddHostedService<KurrentToRabbitForwarder>();
builder.Services.AddScoped<KurrentCheckpointStore>();
```

```json
// appsettings.Development.json
"KurrentForwarder": {
  "CheckpointId": "kurrent-to-rabbit-forwarder",
  "StartFromBeginningWhenNoCheckpoint": true
}
```

- [ ] **Step 5: Run tests and commit**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Worker.Domain tests/WiSave.Expenses.Worker.Domain.Tests
git commit -m "feat(worker-domain): forward committed kurrent events to rabbitmq"
```

### Task 3: Add Projection Idempotency and Transaction Boundaries

**Context and Problem:**
- Once forwarding is enabled, RabbitMQ redelivery and replay are normal behavior.
- Current projection handlers use insert/update patterns that are not dedup-protected.
- Multiple `SaveChangesAsync` calls inside one message handling path allow partial projection mutations.

**Why this task exists:**
- Make projections safe under at-least-once delivery.
- Eliminate double-apply bugs and partial-write states.
- Align projection behavior with event-sourcing replay semantics.

**Success criteria:**
- Duplicate `MessageId` deliveries do not change projection state after first success.
- Handler writes are atomic per consumed message.
- Schema enforces uniqueness where projection invariants require it.

**Files:**
- Create: `src/WiSave.Expenses.Projections/ReadModels/ProcessedMessageReadModel.cs`
- Modify: `src/WiSave.Expenses.Projections/ProjectionsDbContext.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/AccountEventHandler.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/BudgetEventHandler.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
- Create: `src/WiSave.Expenses.Projections.Migrations/Scripts/003_ProcessedMessagesAndProjectionConstraints.sql`
- Create: `tests/WiSave.Expenses.Projections.Tests/EventHandlers/ProjectionIdempotencyTests.cs`

- [ ] **Step 1: Write failing idempotency test**

```csharp
public class ProjectionIdempotencyTests
{
    [Fact]
    public async Task Duplicate_message_id_is_ignored_on_second_delivery()
    {
        // deliver same AccountOpened twice with same MessageId
        // assert only one account row exists
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal --filter ProjectionIdempotencyTests`  
Expected: FAIL with duplicate insert / missing dedup logic.

- [ ] **Step 3: Implement dedup model and DB schema constraints**

```csharp
public sealed class ProcessedMessageReadModel
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
```

```csharp
modelBuilder.Entity<ProcessedMessageReadModel>(e =>
{
    e.ToTable("processed_messages");
    e.HasKey(x => x.MessageId);
});
```

```sql
CREATE TABLE IF NOT EXISTS projections.processed_messages (
    "MessageId" uuid NOT NULL,
    "ProcessedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_processed_messages" PRIMARY KEY ("MessageId")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_spending_summaries_UserId_Month_Year_CategoryId"
ON projections.spending_summaries ("UserId","Month","Year","CategoryId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_monthly_stats_UserId_Year_Month"
ON projections.monthly_stats ("UserId","Year","Month");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_budget_category_limits_BudgetId_CategoryId"
ON projections.budget_category_limits ("BudgetId","CategoryId");
```

- [ ] **Step 4: Make each projection consumer transactional + deduplicated**

```csharp
var messageId = context.MessageId;
if (messageId is null) throw new InvalidOperationException("MessageId header is required.");

await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);
var alreadyProcessed = await db.Set<ProcessedMessageReadModel>()
    .AnyAsync(x => x.MessageId == messageId.Value, context.CancellationToken);
if (alreadyProcessed) return;

// existing projection mutations...

db.Set<ProcessedMessageReadModel>().Add(new ProcessedMessageReadModel
{
    MessageId = messageId.Value,
    ProcessedAt = DateTimeOffset.UtcNow
});
await db.SaveChangesAsync(context.CancellationToken);
await tx.CommitAsync(context.CancellationToken);
```

- [ ] **Step 5: Run tests and commit**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Projections src/WiSave.Expenses.Projections.Migrations tests/WiSave.Expenses.Projections.Tests
git commit -m "fix(projections): add idempotent message processing and transactional writes"
```

### Task 4: Correct ExpenseUpdated Projection Recalculation Rules

**Context and Problem:**
- Graph call edges highlight `ExpenseEventHandler.Consume` as central to summary/stat updates.
- Existing logic recalculates only on amount changes, missing category/date moves.
- This causes silent read-model drift despite correct event history.

**Why this task exists:**
- Ensure summary and monthly stats are mathematically correct for all update shapes.
- Prevent category/month totals from diverging during recategorization and date shifts.

**Success criteria:**
- Recategorization without amount change moves totals across categories correctly.
- Date/month changes without amount change move totals across periods correctly.
- Combined changes (amount + category + date) maintain net correctness.

**Files:**
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
- Create: `tests/WiSave.Expenses.Projections.Tests/EventHandlers/ExpenseUpdatedRecalculationTests.cs`

- [ ] **Step 1: Write failing tests for category/date-only and amount+category moves**

```csharp
[Fact]
public async Task ExpenseUpdated_recategorize_moves_spend_between_categories_without_amount_change()
{
    // old category A 100 -> new category B 100
    // assert A decremented by 100 and B incremented by 100
}

[Fact]
public async Task ExpenseUpdated_date_change_moves_spend_between_months_without_amount_change()
{
    // old month March -> new month April
    // assert March -amount and April +amount
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal --filter ExpenseUpdatedRecalculationTests`  
Expected: FAIL because existing handler recalculates only when amount changes.

- [ ] **Step 3: Implement old/new tuple delta application**

```csharp
var oldCategory = old.CategoryId;
var oldMonth = old.Date.Month;
var oldYear = old.Date.Year;
var oldAmount = old.Amount;

// apply incoming changes to expense entity first...

var newCategory = expense.CategoryId;
var newMonth = expense.Date.Month;
var newYear = expense.Date.Year;
var newAmount = expense.Amount;

var movedCategory = oldCategory != newCategory;
var movedPeriod = oldMonth != newMonth || oldYear != newYear;
var changedAmount = oldAmount != newAmount;

if (movedCategory || movedPeriod || changedAmount)
{
    await UpdateSpendingSummaryAsync(expense.UserId, oldCategory, oldMonth, oldYear, -oldAmount, ct);
    await UpdateSpendingSummaryAsync(expense.UserId, newCategory, newMonth, newYear, newAmount, ct);
    await UpdateMonthlyStatsAsync(expense.UserId, oldMonth, oldYear, -oldAmount, expense.Currency, ct);
    await UpdateMonthlyStatsAsync(expense.UserId, newMonth, newYear, newAmount, expense.Currency, ct);
}
```

- [ ] **Step 4: Run focused tests and full projection tests**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs tests/WiSave.Expenses.Projections.Tests/EventHandlers/ExpenseUpdatedRecalculationTests.cs
git commit -m "fix(projections): recalculate summaries on recategorization and month moves"
```

### Task 5: Runtime Wiring, Documentation, and End-to-End Verification

**Context and Problem:**
- README architecture claims and runtime behavior currently diverge.
- Operational confidence depends on migration, config, and observable runtime checks.
- Forwarding correctness is incomplete without restart/resume validation.

**Why this task exists:**
- Close documentation/runtime gap.
- Ensure local and containerized environments can run the new flow predictably.
- Produce repeatable validation steps for handoff.

**Success criteria:**
- Docs reflect actual command/event/projection runtime path.
- Local runtime starts with forwarder config enabled.
- Manual smoke flow demonstrates append -> forward -> consume -> projection update and checkpoint resume behavior.

**Files:**
- Modify: `README.md`
- Modify: `docker-compose.yml`
- Modify: `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
- Create: `docs/superpowers/specs/2026-04-11-kurrentdb-rabbitmq-committed-events-runbook.md`

- [ ] **Step 1: Update architecture docs to reflect real flow**

```markdown
- Worker.Domain consumes commands and appends events to KurrentDB.
- Worker.Domain forwarder republishes committed KurrentDB events to RabbitMQ.
- Worker.Projections consumes RabbitMQ events and updates Postgres read models.
```

- [ ] **Step 2: Ensure runtime config includes forwarder settings in local/dev containers**

```yaml
# docker-compose.yml (worker-domain env)
- KurrentForwarder__CheckpointId=kurrent-to-rabbit-forwarder
- KurrentForwarder__StartFromBeginningWhenNoCheckpoint=true
```

- [ ] **Step 3: Execute migration + test + smoke flow**

Run: `dotnet run --project src/WiSave.Expenses.Projections.Migrations -- "Host=postgres;Database=wisave_expenses;Username=wisave;Password=wisave_dev"`  
Expected: `Success!`

Run: `dotnet test WiSave.Expenses.slnx -v minimal`  
Expected: PASS for all test projects with tests.

Run: `docker compose up --build worker-domain worker-projections`  
Expected: forwarder logs show publish acknowledgements and projections receive events.

- [ ] **Step 4: Manual verification scenario**

```text
1) POST /expenses/accounts
2) confirm AccountOpened appended in KurrentDB stream
3) confirm AccountOpened published to RabbitMQ (worker-domain log)
4) confirm projections.accounts row created
5) restart worker-domain and replay one new command; ensure checkpoint prevents republishing old events
```

- [ ] **Step 5: Commit**

```bash
git add README.md docker-compose.yml src/WiSave.Expenses.Worker.Domain/appsettings.Development.json docs/superpowers/specs/2026-04-11-kurrentdb-rabbitmq-committed-events-runbook.md
git commit -m "docs(architecture): document committed-event forwarding and operations runbook"
```

---

## Out-of-Scope Follow-Up (Recommended Next Plan)

- Move domain events out of `Contracts` dependency to keep `Core.Domain` isolated from integration contracts.
- Introduce explicit domain-event-to-integration-event mapper in application/infrastructure boundary.
- Version event type names in stream metadata to prevent silent deserialization drift on contract evolution.

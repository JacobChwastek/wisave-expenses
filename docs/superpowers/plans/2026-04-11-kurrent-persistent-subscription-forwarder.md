# Kurrent Persistent Subscription Forwarder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Forward committed events from KurrentDB to RabbitMQ without Kurrent Connector license, using persistent subscriptions with at-least-once delivery and safe projection handling.

**Architecture:** Keep command handlers unchanged (append to Kurrent only), then run a background forwarder in `Worker.Domain` that reads from a Kurrent persistent subscription and publishes to RabbitMQ. Use subscription ack/nack for delivery control. Harden projection consumers with inbox dedup (`processed_messages`) and transactional writes.

**Tech Stack:** .NET 10, EventStore.Client (streams + persistent subscriptions), MassTransit/RabbitMQ, EF Core 10, PostgreSQL, xUnit.

---

## Preconditions

- No Kurrent Connector license is available.
- `Worker.Domain` can connect to KurrentDB and RabbitMQ.
- Forwarder subscription group will be created automatically by code (or reused if already exists).

---

## File Structure

- `src/WiSave.Expenses.Core.Infrastructure/EventStore/ContractEventTypeRegistry.cs`
Shared resolver of Kurrent event type name -> contract CLR type.

- `src/WiSave.Expenses.Core.Infrastructure/EventStore/KurrentDbAggregateRepository.cs`
Use shared resolver for aggregate rehydration.

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentForwarderOptions.cs`
Forwarder config: subscription group, stream scope, checkpoint mode.

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentSubscriptionBootstrapper.cs`
Create persistent subscription group to `$all` (idempotent create).

- `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentToRabbitForwarder.cs`
Hosted service that subscribes, maps events, publishes to Rabbit, ack/nack.

- `src/WiSave.Expenses.Worker.Domain/Program.cs`
Register bootstrapper + forwarder + options.

- `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
Forwarder configuration.

- `src/WiSave.Expenses.Core.Infrastructure/WiSave.Expenses.Core.Infrastructure.csproj`
Add persistent subscriptions package if missing.

- `src/WiSave.Expenses.Projections/ReadModels/ProcessedMessageReadModel.cs`
Inbox dedup model.

- `src/WiSave.Expenses.Projections/ProjectionsDbContext.cs`
Map dedup table and add uniqueness constraints.

- `src/WiSave.Expenses.Projections/EventHandlers/AccountEventHandler.cs`
- `src/WiSave.Expenses.Projections/EventHandlers/BudgetEventHandler.cs`
- `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
Transaction + dedup + corrected recalculation logic.

- `src/WiSave.Expenses.Projections.Migrations/Scripts/003_ProcessedMessagesAndUniqueIndexes.sql`
Schema for dedup + projection uniqueness.

- `tests/WiSave.Expenses.Worker.Domain.Tests/*`
Forwarder and bootstrap tests.

- `tests/WiSave.Expenses.Projections.Tests/*`
Idempotency and `ExpenseUpdated` correctness tests.

- `README.md`
Update runtime flow description.

- `WiSave.Expenses.slnx`
Add new test project if introduced.

---

### Task 1: Centralize Contract Event Type Resolution

**Context and explanation:**  
Forwarder and aggregate repository both deserialize by event type name. One resolver prevents drift.

**Files:**
- Create: `src/WiSave.Expenses.Core.Infrastructure/EventStore/ContractEventTypeRegistry.cs`
- Modify: `src/WiSave.Expenses.Core.Infrastructure/EventStore/KurrentDbAggregateRepository.cs`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/EventStore/ContractEventTypeRegistryTests.cs`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj`
- Modify: `WiSave.Expenses.slnx`

- [ ] **Step 1: Write failing test**

```csharp
using WiSave.Expenses.Core.Infrastructure.EventStore;

namespace WiSave.Expenses.Worker.Domain.Tests.EventStore;

public class ContractEventTypeRegistryTests
{
    [Fact]
    public void Resolve_known_event_name_returns_type()
    {
        var sut = new ContractEventTypeRegistry();
        var type = sut.Resolve("AccountOpened");
        Assert.Equal("WiSave.Expenses.Contracts.Events.Accounts.AccountOpened", type?.FullName);
    }
}
```

- [ ] **Step 2: Run test and confirm fail**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter ContractEventTypeRegistryTests`  
Expected: FAIL (type missing).

- [ ] **Step 3: Implement registry and wire repository**

```csharp
namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public sealed class ContractEventTypeRegistry
{
    private readonly Dictionary<string, Type> _map;

    public ContractEventTypeRegistry()
    {
        var assembly = typeof(WiSave.Expenses.Contracts.Events.CommandFailed).Assembly;
        _map = assembly.GetExportedTypes()
            .Where(t => t.Namespace?.Contains(".Events.") == true)
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
    }

    public Type? Resolve(string eventTypeName) =>
        _map.TryGetValue(eventTypeName, out var type) ? type : null;
}
```

- [ ] **Step 4: Verify pass**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter ContractEventTypeRegistryTests`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add WiSave.Expenses.slnx src/WiSave.Expenses.Core.Infrastructure/EventStore tests/WiSave.Expenses.Worker.Domain.Tests
git commit -m "refactor(eventstore): add shared contract event type registry"
```

---

### Task 2: Add Persistent Subscription Bootstrap

**Context and explanation:**  
Without connector license, Kurrent persistent subscription provides server-side checkpointing, retries, and consumer coordination. Bootstrapper guarantees group exists.

**Files:**
- Modify: `src/WiSave.Expenses.Core.Infrastructure/WiSave.Expenses.Core.Infrastructure.csproj`
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentForwarderOptions.cs`
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentSubscriptionBootstrapper.cs`
- Modify: `src/WiSave.Expenses.Worker.Domain/Program.cs`
- Modify: `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/Forwarding/KurrentSubscriptionBootstrapperTests.cs`

- [ ] **Step 1: Write failing bootstrap test**

```csharp
namespace WiSave.Expenses.Worker.Domain.Tests.Forwarding;

public class KurrentSubscriptionBootstrapperTests
{
    [Fact]
    public async Task EnsureCreatedAsync_is_idempotent_when_group_exists()
    {
        // Arrange fake persistent-subscriptions client that throws "already exists" on create
        // Act: EnsureCreatedAsync
        // Assert: method completes without exception
    }
}
```

- [ ] **Step 2: Run and confirm fail**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter KurrentSubscriptionBootstrapperTests`  
Expected: FAIL (bootstrapper missing).

- [ ] **Step 3: Implement options + bootstrapper**

```csharp
public sealed class KurrentForwarderOptions
{
    public string GroupName { get; set; } = "expenses-forwarder";
    public bool FromStartWhenCreated { get; set; } = true;
}
```

```csharp
using EventStore.Client;

public sealed class KurrentSubscriptionBootstrapper(
    EventStorePersistentSubscriptionsClient persistentClient,
    IOptions<KurrentForwarderOptions> options,
    ILogger<KurrentSubscriptionBootstrapper> logger)
{
    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        var group = options.Value.GroupName;
        var settings = options.Value.FromStartWhenCreated
            ? PersistentSubscriptionSettings.Create().StartFromBeginning()
            : PersistentSubscriptionSettings.Create().StartFromCurrent();

        try
        {
            await persistentClient.CreateToAllAsync(group, settings, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            logger.LogInformation("Persistent subscription group {Group} already exists.", group);
        }
    }
}
```

- [ ] **Step 4: Wire DI + config**

Run code changes:
- Add package `EventStore.Client.Grpc.PersistentSubscriptions` in infrastructure csproj.
- Register `KurrentForwarderOptions` binding in `Program.cs`.

Config:

```json
"KurrentForwarder": {
  "GroupName": "expenses-forwarder",
  "FromStartWhenCreated": true
}
```

- [ ] **Step 5: Verify and commit**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter KurrentSubscriptionBootstrapperTests`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Worker.Domain src/WiSave.Expenses.Core.Infrastructure tests/WiSave.Expenses.Worker.Domain.Tests
git commit -m "feat(worker-domain): add persistent subscription bootstrap for event forwarding"
```

---

### Task 3: Implement Kurrent -> Rabbit Forwarder (Ack/Nack)

**Context and explanation:**  
This is the custom “connector” runtime. It forwards only committed events and uses ack/nack for delivery guarantees.

**Files:**
- Create: `src/WiSave.Expenses.Worker.Domain/Forwarding/KurrentToRabbitForwarder.cs`
- Modify: `src/WiSave.Expenses.Worker.Domain/Program.cs`
- Create: `tests/WiSave.Expenses.Worker.Domain.Tests/Forwarding/KurrentToRabbitForwarderTests.cs`

- [ ] **Step 1: Write failing behavior tests**

```csharp
public class KurrentToRabbitForwarderTests
{
    [Fact]
    public async Task Known_event_is_published_and_acked() { }

    [Fact]
    public async Task Unknown_event_is_acked_without_publish() { }

    [Fact]
    public async Task Publish_failure_nacks_for_retry() { }
}
```

- [ ] **Step 2: Run and confirm fail**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter KurrentToRabbitForwarderTests`  
Expected: FAIL (forwarder missing).

- [ ] **Step 3: Implement hosted forwarder**

```csharp
using System.Text;
using System.Text.Json;
using EventStore.Client;
using MassTransit;

public sealed class KurrentToRabbitForwarder : BackgroundService
{
    private readonly EventStorePersistentSubscriptionsClient _persistent;
    private readonly KurrentSubscriptionBootstrapper _bootstrapper;
    private readonly ContractEventTypeRegistry _types;
    private readonly IPublishEndpoint _publish;
    private readonly IOptions<KurrentForwarderOptions> _options;
    private readonly ILogger<KurrentToRabbitForwarder> _logger;

    public KurrentToRabbitForwarder(
        EventStorePersistentSubscriptionsClient persistent,
        KurrentSubscriptionBootstrapper bootstrapper,
        ContractEventTypeRegistry types,
        IPublishEndpoint publish,
        IOptions<KurrentForwarderOptions> options,
        ILogger<KurrentToRabbitForwarder> logger)
    {
        _persistent = persistent;
        _bootstrapper = bootstrapper;
        _types = types;
        _publish = publish;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _bootstrapper.EnsureCreatedAsync(stoppingToken);

        await _persistent.SubscribeToAllAsync(
            _options.Value.GroupName,
            eventAppeared: async (sub, resolved, retryCount, ct) =>
            {
                try
                {
                    if (resolved.Event.EventType.StartsWith("$", StringComparison.Ordinal))
                    {
                        await sub.Ack(resolved);
                        return;
                    }

                    var clrType = _types.Resolve(resolved.Event.EventType);
                    if (clrType is null)
                    {
                        _logger.LogWarning("Skipping unknown event type {EventType}.", resolved.Event.EventType);
                        await sub.Ack(resolved);
                        return;
                    }

                    var payload = Encoding.UTF8.GetString(resolved.Event.Data.Span);
                    var message = JsonSerializer.Deserialize(payload, clrType);
                    if (message is null)
                    {
                        await sub.Ack(resolved);
                        return;
                    }

                    await _publish.Publish(message, pipe =>
                    {
                        pipe.MessageId = resolved.Event.EventId.ToGuid();
                    }, ct);

                    await sub.Ack(resolved);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Forwarding failed for {EventType}.", resolved.Event.EventType);
                    await sub.Nack(PersistentSubscriptionNakEventAction.Retry, ex.Message, resolved);
                }
            },
            cancellationToken: stoppingToken);
    }
}
```

- [ ] **Step 4: Register hosted service**

Add in `Program.cs`:

```csharp
builder.Services.AddHostedService<KurrentToRabbitForwarder>();
builder.Services.AddSingleton<ContractEventTypeRegistry>();
builder.Services.AddSingleton<KurrentSubscriptionBootstrapper>();
```

- [ ] **Step 5: Verify and commit**

Run: `dotnet test tests/WiSave.Expenses.Worker.Domain.Tests/WiSave.Expenses.Worker.Domain.Tests.csproj -v minimal --filter KurrentToRabbitForwarderTests`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Worker.Domain tests/WiSave.Expenses.Worker.Domain.Tests
git commit -m "feat(worker-domain): forward committed kurrent events to rabbit via persistent subscription"
```

---

### Task 4: Add Projection Inbox Dedup + Transactional Handling

**Context and explanation:**  
Forwarder guarantees at-least-once. Rabbit can redeliver. Projections must be idempotent and atomic per message.

**Files:**
- Create: `src/WiSave.Expenses.Projections/ReadModels/ProcessedMessageReadModel.cs`
- Modify: `src/WiSave.Expenses.Projections/ProjectionsDbContext.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/AccountEventHandler.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/BudgetEventHandler.cs`
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
- Create: `src/WiSave.Expenses.Projections.Migrations/Scripts/003_ProcessedMessagesAndUniqueIndexes.sql`
- Create: `tests/WiSave.Expenses.Projections.Tests/EventHandlers/ProjectionIdempotencyTests.cs`

- [ ] **Step 1: Write failing dedup test**

```csharp
[Fact]
public async Task Duplicate_message_id_does_not_apply_projection_twice()
{
    // send AccountOpened with same MessageId twice
    // assert single account row and single processed_messages row
}
```

- [ ] **Step 2: Run and confirm fail**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal --filter ProjectionIdempotencyTests`  
Expected: FAIL.

- [ ] **Step 3: Implement inbox + schema**

`ProcessedMessageReadModel`:

```csharp
public sealed class ProcessedMessageReadModel
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
```

SQL migration:

```sql
CREATE TABLE IF NOT EXISTS projections.processed_messages (
    "MessageId" uuid PRIMARY KEY,
    "ProcessedAt" timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_budget_category_limits_BudgetId_CategoryId"
ON projections.budget_category_limits ("BudgetId","CategoryId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_spending_summaries_UserId_Month_Year_CategoryId"
ON projections.spending_summaries ("UserId","Month","Year","CategoryId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_monthly_stats_UserId_Year_Month"
ON projections.monthly_stats ("UserId","Year","Month");
```

- [ ] **Step 4: Apply dedup+transaction template in each handler**

```csharp
var messageId = context.MessageId ?? throw new InvalidOperationException("MessageId is required.");
await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);
if (await db.Set<ProcessedMessageReadModel>().AnyAsync(x => x.MessageId == messageId, context.CancellationToken))
    return;

// apply projection mutation(s)

db.Set<ProcessedMessageReadModel>().Add(new ProcessedMessageReadModel
{
    MessageId = messageId,
    ProcessedAt = DateTimeOffset.UtcNow
});
await db.SaveChangesAsync(context.CancellationToken);
await tx.CommitAsync(context.CancellationToken);
```

- [ ] **Step 5: Verify and commit**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Projections src/WiSave.Expenses.Projections.Migrations tests/WiSave.Expenses.Projections.Tests
git commit -m "fix(projections): add inbox dedup and transactional consumer writes"
```

---

### Task 5: Fix ExpenseUpdated Recalculation Drift

**Context and explanation:**  
Current `ExpenseUpdated` projection logic recalculates only when amount changes. Category/date moves without amount changes currently drift summaries.

**Files:**
- Modify: `src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs`
- Create: `tests/WiSave.Expenses.Projections.Tests/EventHandlers/ExpenseUpdatedRecalculationTests.cs`

- [ ] **Step 1: Write failing behavior tests**

```csharp
[Fact]
public async Task Recategorization_without_amount_change_moves_totals_between_categories() { }

[Fact]
public async Task Date_change_without_amount_change_moves_totals_between_months() { }
```

- [ ] **Step 2: Run and confirm fail**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal --filter ExpenseUpdatedRecalculationTests`  
Expected: FAIL.

- [ ] **Step 3: Implement old/new full delta recalculation**

```csharp
var oldCategory = expense.CategoryId;
var oldMonth = expense.Date.Month;
var oldYear = expense.Date.Year;
var oldAmount = expense.Amount;

// apply updates...

var newCategory = expense.CategoryId;
var newMonth = expense.Date.Month;
var newYear = expense.Date.Year;
var newAmount = expense.Amount;

if (oldCategory != newCategory || oldMonth != newMonth || oldYear != newYear || oldAmount != newAmount)
{
    await UpdateSpendingSummaryAsync(expense.UserId, oldCategory, oldMonth, oldYear, -oldAmount, context.CancellationToken);
    await UpdateSpendingSummaryAsync(expense.UserId, newCategory, newMonth, newYear, newAmount, context.CancellationToken);
    await UpdateMonthlyStatsAsync(expense.UserId, oldMonth, oldYear, -oldAmount, expense.Currency, context.CancellationToken);
    await UpdateMonthlyStatsAsync(expense.UserId, newMonth, newYear, newAmount, expense.Currency, context.CancellationToken);
}
```

- [ ] **Step 4: Verify and commit**

Run: `dotnet test tests/WiSave.Expenses.Projections.Tests/WiSave.Expenses.Projections.Tests.csproj -v minimal`  
Expected: PASS.

```bash
git add src/WiSave.Expenses.Projections/EventHandlers/ExpenseEventHandler.cs tests/WiSave.Expenses.Projections.Tests/EventHandlers/ExpenseUpdatedRecalculationTests.cs
git commit -m "fix(projections): correct expense-updated summary/monthly recalculation"
```

---

### Task 6: End-to-End Validation and Documentation

**Context and explanation:**  
Need proof that append -> forward -> consume path works after restart, with idempotency preserved.

**Files:**
- Modify: `README.md`
- Modify: `docker-compose.yml`
- Modify: `src/WiSave.Expenses.Worker.Domain/appsettings.Development.json`
- Create: `docs/superpowers/specs/2026-04-11-kurrent-forwarder-runbook.md`

- [ ] **Step 1: Update architecture docs**

Add:
- WebApi publishes commands to RabbitMQ.
- Worker.Domain handles commands and appends events to KurrentDB.
- Worker.Domain forwarder republishes committed Kurrent events via persistent subscription.
- Worker.Projections consumes Rabbit messages and updates projections.

- [ ] **Step 2: Add runtime config**

Ensure `docker-compose.yml` includes forwarder config env:

```yaml
- KurrentForwarder__GroupName=expenses-forwarder
- KurrentForwarder__FromStartWhenCreated=true
```

- [ ] **Step 3: Run migration + full tests**

Run: `dotnet run --project src/WiSave.Expenses.Projections.Migrations -- "Host=postgres;Database=wisave_expenses;Username=wisave;Password=wisave_dev"`  
Expected: `Success!`

Run: `dotnet test WiSave.Expenses.slnx -v minimal`  
Expected: PASS.

- [ ] **Step 4: Smoke scenario**

Run: `docker compose up --build worker-domain worker-projections wisave-expenses-webapi`  
Then:
1. POST account command via API.
2. Confirm stream event in Kurrent.
3. Confirm forwarder publish log entry.
4. Confirm projection row exists.
5. Restart `worker-domain`, send one more command, verify no duplicate projection for prior event.

- [ ] **Step 5: Commit**

```bash
git add README.md docker-compose.yml src/WiSave.Expenses.Worker.Domain/appsettings.Development.json docs/superpowers/specs/2026-04-11-kurrent-forwarder-runbook.md
git commit -m "docs(runtime): add kurrent persistent-subscription forwarding runbook"
```

---

## Out-of-Scope Follow-Up

- Separate domain events from integration contracts (`Core.Domain` no dependency on `Contracts` events namespace).
- Add explicit event-versioning strategy in event metadata for long-term compatibility.
- Add DLQ/parking monitor dashboard and operational alerts.

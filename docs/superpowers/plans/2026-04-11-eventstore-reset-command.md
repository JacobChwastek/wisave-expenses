# EventStore Reset Command Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an `eventstore-reset` console command that tombstones all non-system streams and deletes all persistent subscriptions in a KurrentDB instance.

**Architecture:** New `EventStoreResetCommand` following the existing command pattern (`IExpensesCommand`). Reset logic lives in a separate `EventStoreResetOperations` service. The command creates short-lived EventStore clients from the user-provided connection string — no dependency on `Core.Infrastructure`.

**Tech Stack:** C# / .NET 10, EventStore.Client.Grpc.Streams 23.3.9, EventStore.Client.Grpc.PersistentSubscriptions 23.3.9

**Spec:** `docs/superpowers/specs/2026-04-11-eventstore-reset-command-design.md`

---

### File Map

| File | Action | Responsibility |
| ---- | ------ | -------------- |
| `src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj` | Modify | Add EventStore NuGet packages |
| `src/WiSave.Expenses.Console/Operations/EventStoreResetOperations.cs` | Create | Interface + implementation for reset logic |
| `src/WiSave.Expenses.Console/Commands/EventStoreResetCommand.cs` | Create | `IExpensesCommand` implementation |
| `src/WiSave.Expenses.Console/Infrastructure/ServiceCollectionExtensions.cs` | Modify | Register `IEventStoreResetOperations` |

---

### Task 1: Add EventStore NuGet packages to Console project

**Files:**
- Modify: `src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj`

- [ ] **Step 1: Add package references**

Add to the `<ItemGroup>` with package references in `src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj`:

```xml
<PackageReference Include="EventStore.Client.Grpc.Streams" Version="23.3.9" />
<PackageReference Include="EventStore.Client.Grpc.PersistentSubscriptions" Version="23.3.9" />
```

The csproj currently has no `<ItemGroup>` with `<PackageReference>` elements (only `Microsoft.Extensions.Hosting`). Add the new references to the existing `<ItemGroup>` that contains `Microsoft.Extensions.Hosting`.

- [ ] **Step 2: Verify restore succeeds**

Run:
```bash
dotnet restore src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj
```
Expected: restore succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj
git commit -m "chore(console): add EventStore client packages for reset command"
```

---

### Task 2: Create EventStoreResetOperations

**Files:**
- Create: `src/WiSave.Expenses.Console/Operations/EventStoreResetOperations.cs`

- [ ] **Step 1: Create the interface and implementation**

Create `src/WiSave.Expenses.Console/Operations/EventStoreResetOperations.cs`:

```csharp
using System.Text;
using EventStore.Client;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Operations;

internal sealed record EventStoreResetResult(
    int StreamsTombstoned,
    int SubscriptionsDeleted,
    IReadOnlyList<string> Errors)
{
    public string Format()
    {
        var builder = new StringBuilder();
        builder.Append($"Tombstoned {StreamsTombstoned} stream(s).");
        builder.AppendLine();
        builder.Append($"Deleted {SubscriptionsDeleted} persistent subscription(s).");

        if (Errors.Count > 0)
        {
            builder.AppendLine();
            builder.Append($"{Errors.Count} error(s) encountered:");
            foreach (var error in Errors)
            {
                builder.AppendLine();
                builder.Append($"  - {error}");
            }
        }

        return builder.ToString();
    }
}

internal interface IEventStoreResetOperations
{
    Task<EventStoreResetResult> RunAsync(string connectionString, IConsoleOutput consoleOutput, CancellationToken ct);
}

internal sealed class EventStoreResetOperations : IEventStoreResetOperations
{
    public async Task<EventStoreResetResult> RunAsync(string connectionString, IConsoleOutput consoleOutput, CancellationToken ct)
    {
        var settings = EventStoreClientSettings.Create(connectionString);
        await using var streamClient = new EventStoreClient(settings);
        await using var subscriptionClient = new EventStorePersistentSubscriptionsClient(settings);

        var (tombstoned, tombstoneErrors) = await TombstoneAllStreamsAsync(streamClient, consoleOutput, ct);
        var (deleted, subscriptionErrors) = await DeleteAllPersistentSubscriptionsAsync(subscriptionClient, consoleOutput, ct);

        var allErrors = tombstoneErrors.Concat(subscriptionErrors).ToList();
        return new EventStoreResetResult(tombstoned, deleted, allErrors);
    }

    private static async Task<(int Count, List<string> Errors)> TombstoneAllStreamsAsync(
        EventStoreClient client,
        IConsoleOutput consoleOutput,
        CancellationToken ct)
    {
        consoleOutput.WriteLine("Reading $all to discover streams...");

        var streamNames = new HashSet<string>(StringComparer.Ordinal);
        var readResult = client.ReadAllAsync(Direction.Forwards, Position.Start, cancellationToken: ct);

        await foreach (var resolved in readResult)
        {
            var name = resolved.OriginalStreamId;
            if (!name.StartsWith('$'))
            {
                streamNames.Add(name);
            }
        }

        consoleOutput.WriteLine($"Discovered {streamNames.Count} non-system stream(s).");

        var tombstoned = 0;
        var errors = new List<string>();

        foreach (var streamName in streamNames)
        {
            try
            {
                await client.TombstoneAsync(streamName, StreamState.Any, cancellationToken: ct);
                tombstoned++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to tombstone '{streamName}': {ex.Message}");
            }
        }

        consoleOutput.WriteLine($"Tombstoned {tombstoned}/{streamNames.Count} stream(s).");
        return (tombstoned, errors);
    }

    private static async Task<(int Count, List<string> Errors)> DeleteAllPersistentSubscriptionsAsync(
        EventStorePersistentSubscriptionsClient client,
        IConsoleOutput consoleOutput,
        CancellationToken ct)
    {
        consoleOutput.WriteLine("Listing persistent subscriptions...");

        IReadOnlyList<PersistentSubscriptionInfo> subscriptions;
        try
        {
            var result = client.ListAllAsync(cancellationToken: ct);
            subscriptions = await result.ToListAsync(ct);
        }
        catch (Exception ex)
        {
            return (0, [$"Failed to list persistent subscriptions: {ex.Message}"]);
        }

        consoleOutput.WriteLine($"Found {subscriptions.Count} persistent subscription(s).");

        var deleted = 0;
        var errors = new List<string>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                if (subscription.EventSource == "$all")
                {
                    await client.DeleteToAllAsync(subscription.GroupName, cancellationToken: ct);
                }
                else
                {
                    await client.DeleteToStreamAsync(subscription.EventSource, subscription.GroupName, cancellationToken: ct);
                }

                deleted++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to delete subscription '{subscription.GroupName}' on '{subscription.EventSource}': {ex.Message}");
            }
        }

        consoleOutput.WriteLine($"Deleted {deleted}/{subscriptions.Count} persistent subscription(s).");
        return (deleted, errors);
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
dotnet build src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj --no-restore
```
Expected: build succeeds. If `ListAllAsync`, `DeleteToAllAsync`, or `DeleteToStreamAsync` don't exist in this client version, check the available API surface — the v23.x client may use `ListAsync` or similar. Adjust method names to match the actual API.

- [ ] **Step 3: Commit**

```bash
git add src/WiSave.Expenses.Console/Operations/EventStoreResetOperations.cs
git commit -m "feat(console): add EventStoreResetOperations for full KurrentDB reset"
```

---

### Task 3: Create EventStoreResetCommand

**Files:**
- Create: `src/WiSave.Expenses.Console/Commands/EventStoreResetCommand.cs`

- [ ] **Step 1: Create the command**

Create `src/WiSave.Expenses.Console/Commands/EventStoreResetCommand.cs`:

```csharp
using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Commands;

internal sealed class EventStoreResetCommand(
    IEventStoreResetOperations resetOperations,
    IConsoleOutput consoleOutput) : IExpensesCommand
{
    private static readonly IReadOnlyList<CommandParameter> Parameters =
    [
        new("connection-string", "KurrentDB connection string (e.g. esdb://localhost:2113?tls=false).", true)
    ];

    public string Name => "eventstore-reset";

    public string Description => "Permanently tombstone all streams and delete all subscriptions in a KurrentDB instance.";

    public IReadOnlyList<CommandParameter> ParameterDefinitions => Parameters;

    public async Task<CommandResult> ExecuteAsync(CommandExecutionContext context, CancellationToken ct)
    {
        var connectionString = context.GetArgument("connection-string");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return CommandResult.FailureResult("--connection-string is required.");
        }

        if (context.AllowPrompting)
        {
            consoleOutput.WriteLine("WARNING: This will permanently tombstone ALL non-system streams");
            consoleOutput.WriteLine("and delete ALL persistent subscriptions. This is IRREVERSIBLE.");
            consoleOutput.WriteLine($"Target: {connectionString}");
            consoleOutput.Write("Type 'yes' to confirm: ");

            var confirmation = consoleOutput.ReadLine()?.Trim();
            if (!string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.SuccessResult("EventStore reset cancelled.");
            }
        }

        try
        {
            var result = await resetOperations.RunAsync(connectionString, consoleOutput, ct);

            return result.Errors.Count > 0
                ? CommandResult.FailureResult(result.Format())
                : CommandResult.SuccessResult(result.Format());
        }
        catch (Exception ex)
        {
            return CommandResult.FailureResult($"EventStore reset failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
dotnet build src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj --no-restore
```
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/WiSave.Expenses.Console/Commands/EventStoreResetCommand.cs
git commit -m "feat(console): add eventstore-reset command"
```

---

### Task 4: Register operations in DI

**Files:**
- Modify: `src/WiSave.Expenses.Console/Infrastructure/ServiceCollectionExtensions.cs:21`

- [ ] **Step 1: Add the DI registration**

In `src/WiSave.Expenses.Console/Infrastructure/ServiceCollectionExtensions.cs`, add the following line after the existing `IDatabaseMigrationOperations` registration (line 23):

```csharp
services.AddSingleton<IEventStoreResetOperations, EventStoreResetOperations>();
```

Also add the using at the top if not already present (it should already be there since `Operations` namespace is already imported).

- [ ] **Step 2: Verify it compiles**

Run:
```bash
dotnet build src/WiSave.Expenses.Console/WiSave.Expenses.Console.csproj --no-restore
```
Expected: build succeeds.

- [ ] **Step 3: Verify full solution builds**

Run:
```bash
dotnet build
```
Expected: full solution build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/WiSave.Expenses.Console/Infrastructure/ServiceCollectionExtensions.cs
git commit -m "feat(console): register EventStoreResetOperations in DI"
```

---

### Task 5: Smoke test

- [ ] **Step 1: Verify the command shows up in help**

Run:
```bash
dotnet run --project src/WiSave.Expenses.Console -- help
```
Expected: `eventstore-reset` appears in the command list with its description.

- [ ] **Step 2: Verify missing connection-string is handled**

Run:
```bash
dotnet run --project src/WiSave.Expenses.Console -- eventstore-reset
```
Expected: error message about missing `--connection-string` parameter.

- [ ] **Step 3: Commit (no changes expected, just verification)**

No commit needed — this is a verification step only.

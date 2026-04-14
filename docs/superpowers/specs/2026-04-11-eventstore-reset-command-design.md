# EventStore Reset Command

## Overview

A new Console command (`eventstore-reset`) that performs a full, irreversible reset of all data in a KurrentDB (EventStoreDB) instance. Intended for dev/local use. Tombstones every non-system stream and deletes all persistent subscriptions.

## Command Specification

- **Name:** `eventstore-reset`
- **Description:** Permanently reset all streams and subscriptions in a KurrentDB instance.

### Parameters

| Parameter           | Required | Description                                           |
| ------------------- | -------- | ----------------------------------------------------- |
| `connection-string` | Yes      | ESDB connection string (e.g. `esdb://localhost:2113?tls=false`) |

No config-file fallback. The connection string is always explicit to prevent accidental execution against the wrong target.

## Execution Flow

1. **Validate** that `connection-string` argument is provided. Fail fast if missing.
2. **Prompt** (when interactive): display the target connection string, warn that this operation is irreversible and will tombstone all streams. Require `y`/`yes` confirmation.
3. **Connect** to KurrentDB using `EventStoreClient` and `EventStorePersistentSubscriptionsClient` created from the provided connection string.
4. **Discover streams:** Read `$all` forwards from `Position.Start`. Collect all distinct `OriginalStreamId` values. Filter out system streams (names starting with `$`).
5. **Tombstone streams:** For each discovered stream, call `client.TombstoneAsync(streamName, StreamState.Any)`. Collect successes and failures.
6. **Delete persistent subscriptions:** List all persistent subscriptions via the persistent subscriptions client. Delete each one.
7. **Report results:** Return a summary — number of streams tombstoned, number of subscriptions deleted, and any errors encountered.

## Architecture

Follows the existing command pattern established by `DatabaseMigrateCommand`.

### New Files

| File | Purpose |
| ---- | ------- |
| `Commands/EventStoreResetCommand.cs` | `IExpensesCommand` implementation. Handles parameter validation, user prompting, and delegates to operations. |
| `Operations/IEventStoreResetOperations.cs` | Interface for the reset operations. |
| `Operations/EventStoreResetOperations.cs` | Implementation. Creates EventStore clients from connection string, enumerates streams from `$all`, tombstones streams, deletes persistent subscriptions. |

### Registration

No manual DI registration needed for the command — the existing assembly-scanning in `ServiceCollectionExtensions.RegisterCommands` picks it up automatically.

`IEventStoreResetOperations` / `EventStoreResetOperations` must be registered as a singleton in `ServiceCollectionExtensions.AddExpensesConsole()`.

### Dependencies

Add to `WiSave.Expenses.Console.csproj`:

```xml
<PackageReference Include="EventStore.Client.Grpc.Streams" Version="23.3.9" />
<PackageReference Include="EventStore.Client.Grpc.PersistentSubscriptions" Version="23.3.9" />
```

No project reference to `Core.Infrastructure` is needed — the command creates its own short-lived clients from the provided connection string.

## Error Handling

- Missing `connection-string`: return `CommandResult.FailureResult` immediately.
- Individual stream tombstone failures: log and continue. Report failures in the summary.
- Persistent subscription deletion failures: log and continue. Report failures in the summary.
- Connection failure: catch and return `CommandResult.FailureResult` with the exception message.

## Testing

Unit tests for `EventStoreResetOperations` are not practical without a running EventStoreDB instance. The command itself is thin orchestration. If tests are desired, they would be integration tests against a Docker EventStoreDB container, which is out of scope for this task.

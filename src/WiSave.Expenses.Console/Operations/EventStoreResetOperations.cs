using EventStore.Client;
using System.Text;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Operations;

internal sealed record EventStoreResetResult(
    int StreamsTombstoned,
    int SubscriptionsDeleted,
    IReadOnlyList<string> Errors)
{
    public string Format()
    {
        var builder = new StringBuilder("EventStore reset complete.");

        builder.AppendLine();
        builder.Append($"Streams tombstoned: {StreamsTombstoned}");

        builder.AppendLine();
        builder.Append($"Persistent subscriptions deleted: {SubscriptionsDeleted}");

        if (Errors.Count > 0)
        {
            builder.AppendLine();
            builder.Append($"Errors ({Errors.Count}):");
            foreach (var error in Errors)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(error);
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
        ct.ThrowIfCancellationRequested();

        var settings = EventStoreClientSettings.Create(connectionString);

        var streamsTombstoned = 0;
        var subscriptionsDeleted = 0;
        var errors = new List<string>();

        await using var streamsClient = new EventStoreClient(settings);
        await using var subscriptionsClient = new EventStorePersistentSubscriptionsClient(settings);

        // Delete persistent subscriptions first (before tombstoning streams)
        consoleOutput.WriteLine("Listing persistent subscriptions...");
        try
        {
            var subscriptions = (await subscriptionsClient.ListAllAsync(cancellationToken: ct)).ToList();
            consoleOutput.WriteLine($"Found {subscriptions.Count} persistent subscription(s).");

            foreach (var sub in subscriptions)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (sub.EventSource == "$all")
                    {
                        consoleOutput.WriteLine($"Deleting $all subscription '{sub.GroupName}'...");
                        await subscriptionsClient.DeleteToAllAsync(sub.GroupName, cancellationToken: ct);
                    }
                    else
                    {
                        consoleOutput.WriteLine($"Deleting subscription '{sub.GroupName}' on stream '{sub.EventSource}'...");
                        await subscriptionsClient.DeleteToStreamAsync(sub.EventSource, sub.GroupName, cancellationToken: ct);
                    }
                    subscriptionsDeleted++;
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to delete subscription '{sub.GroupName}' on '{sub.EventSource}': {ex.Message}";
                    consoleOutput.WriteLine(msg);
                    errors.Add(msg);
                }
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to list persistent subscriptions: {ex.Message}";
            consoleOutput.WriteLine(msg);
            errors.Add(msg);
        }

        // Discover all non-system streams via $all
        consoleOutput.WriteLine("Reading $all stream to discover user streams...");
        var streamNames = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var allEvents = streamsClient.ReadAllAsync(
                Direction.Forwards,
                Position.Start,
                cancellationToken: ct);

            await foreach (var resolvedEvent in allEvents.WithCancellation(ct))
            {
                var streamName = resolvedEvent.OriginalEvent.EventStreamId;
                if (!streamName.StartsWith('$'))
                {
                    streamNames.Add(streamName);
                }
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to read $all stream: {ex.Message}";
            consoleOutput.WriteLine(msg);
            errors.Add(msg);
        }

        consoleOutput.WriteLine($"Found {streamNames.Count} user stream(s) to tombstone.");

        foreach (var streamName in streamNames)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                consoleOutput.WriteLine($"Tombstoning stream '{streamName}'...");
                await streamsClient.TombstoneAsync(streamName, StreamState.Any, cancellationToken: ct);
                streamsTombstoned++;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to tombstone stream '{streamName}': {ex.Message}";
                consoleOutput.WriteLine(msg);
                errors.Add(msg);
            }
        }

        return new EventStoreResetResult(streamsTombstoned, subscriptionsDeleted, errors);
    }
}

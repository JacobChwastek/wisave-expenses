using System.Text;
using System.Text.Json;
using EventStore.Client;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public sealed class KurrentDbAggregateRepository<T>(
    EventStoreClient client,
    ContractEventTypeRegistry eventTypeRegistry) : IAggregateRepository<T>
    where T : AggregateRoot, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<T?> LoadAsync(string streamId, CancellationToken ct = default)
    {
        var aggregate = new T();
        var events = new List<object>();

        try
        {
            var result = client.ReadStreamAsync(Direction.Forwards, streamId, StreamPosition.Start, cancellationToken: ct);

            await foreach (var resolved in result)
            {
                var eventType = eventTypeRegistry.Resolve(resolved.Event.EventType);
                if (eventType is null) continue;

                var data = Encoding.UTF8.GetString(resolved.Event.Data.Span);
                var @event = JsonSerializer.Deserialize(data, eventType, JsonOptions);
                if (@event is not null)
                    events.Add(@event);
            }
        }
        catch (StreamNotFoundException)
        {
            return null;
        }

        if (events.Count == 0)
            return null;

        aggregate.ReplayEvents(events);
        return aggregate;
    }

    public async Task SaveAsync(T aggregate, CancellationToken ct = default)
    {
        var uncommitted = aggregate.GetUncommittedEvents();
        if (uncommitted.Count == 0) return;

        var streamId = $"{typeof(T).Name.ToLowerInvariant()}-{aggregate.Id}";
        var expectedRevision = aggregate.Version < 0
            ? StreamRevision.None
            : StreamRevision.FromInt64(aggregate.Version);

        var eventData = uncommitted.Select(e =>
        {
            var typeName = e.GetType().Name;
            var json = JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), JsonOptions);
            return new EventData(Uuid.NewUuid(), typeName, json);
        }).ToArray();

        if (aggregate.Version < 0)
        {
            await client.AppendToStreamAsync(streamId, StreamState.NoStream, eventData, cancellationToken: ct);
        }
        else
        {
            await client.AppendToStreamAsync(streamId, expectedRevision, eventData, cancellationToken: ct);
        }

        aggregate.ClearUncommittedEvents();
    }
}

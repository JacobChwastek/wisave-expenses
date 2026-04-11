using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Options;
using WiSave.Expenses.Core.Infrastructure.EventStore;

namespace WiSave.Expenses.Worker.Domain.Forwarding;

public sealed class KurrentToRabbitForwarder(
    IKurrentPersistentSubscriptionClient client,
    KurrentSubscriptionBootstrapper bootstrapper,
    IPublishEndpoint publishEndpoint,
    ContractEventTypeRegistry eventTypeRegistry,
    IOptions<KurrentForwarderOptions> options,
    ILogger<KurrentToRabbitForwarder> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bootstrapper.EnsureCreatedAsync(stoppingToken);
        await using var subscription = await client.SubscribeToAllAsync(options.Value.GroupName, stoppingToken);

        await foreach (var message in subscription.Messages)
        {
            switch (message)
            {
                case KurrentPersistentSubscriptionMessage.Confirmation(var subscriptionId):
                    logger.LogInformation("Kurrent persistent subscription {SubscriptionId} connected", subscriptionId);
                    break;

                case KurrentPersistentSubscriptionMessage.Event(var committedEvent):
                    await HandleEventAsync(committedEvent, stoppingToken);
                    break;
            }
        }
    }

    public async Task<bool> HandleEventAsync(KurrentCommittedEvent committedEvent, CancellationToken ct)
    {
        if (!options.Value.StreamPrefixes.Any(prefix =>
                committedEvent.StreamId.StartsWith(prefix, StringComparison.Ordinal)))
        {
            await committedEvent.Actions.SkipAsync("Event stream is outside forwarder scope.", ct);
            return false;
        }

        var clrType = eventTypeRegistry.Resolve(committedEvent.EventType);
        if (clrType is null)
        {
            logger.LogWarning("Parking unknown committed event type {EventType} from stream {StreamId}", committedEvent.EventType, committedEvent.StreamId);
            await committedEvent.Actions.ParkAsync($"Unknown event type {committedEvent.EventType}.", ct);
            return false;
        }

        try
        {
            var message = JsonSerializer.Deserialize(committedEvent.Data, clrType, JsonOptions);
            if (message is null)
            {
                logger.LogWarning("Parking committed event {EventType} because payload deserialized to null", committedEvent.EventType);
                await committedEvent.Actions.ParkAsync($"Payload for {committedEvent.EventType} deserialized to null.", ct);
                return false;
            }

            await publishEndpoint.Publish(message, publishContext =>
            {
                publishContext.MessageId = committedEvent.EventId;
            }, ct);

            await committedEvent.Actions.AckAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to forward committed event {EventType} from stream {StreamId}", committedEvent.EventType, committedEvent.StreamId);
            await committedEvent.Actions.RetryAsync(ex.Message, ct);
            return false;
        }
    }
}

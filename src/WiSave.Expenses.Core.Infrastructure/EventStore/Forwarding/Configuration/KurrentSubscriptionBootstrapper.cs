using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Configuration;

public sealed class KurrentSubscriptionBootstrapper(
    IKurrentPersistentSubscriptionClient client,
    IOptions<KurrentForwarderOptions> options,
    ILogger<KurrentSubscriptionBootstrapper> logger)
{
    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        try
        {
            await client.CreateToAllAsync(
                options.Value.GroupName,
                new KurrentPersistentSubscriptionCreateOptions(
                    options.Value.FromStartWhenCreated,
                    options.Value.MaxSubscriberCount,
                    options.Value.ConsumerStrategyName),
                ct);
        }
        catch (KurrentPersistentSubscriptionAlreadyExistsException)
        {
            logger.LogInformation("Persistent subscription group {GroupName} already exists", options.Value.GroupName);
        }
    }
}

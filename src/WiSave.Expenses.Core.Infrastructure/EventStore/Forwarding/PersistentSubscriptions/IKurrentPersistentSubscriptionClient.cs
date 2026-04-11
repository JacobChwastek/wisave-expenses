namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public interface IKurrentPersistentSubscriptionClient
{
    Task CreateToAllAsync(string groupName, KurrentPersistentSubscriptionCreateOptions options, CancellationToken ct);
    Task<IKurrentPersistentSubscription> SubscribeToAllAsync(string groupName, CancellationToken ct);
}

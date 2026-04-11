namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public interface IKurrentPersistentSubscription : IAsyncDisposable
{
    IAsyncEnumerable<KurrentPersistentSubscriptionMessage> Messages { get; }
}

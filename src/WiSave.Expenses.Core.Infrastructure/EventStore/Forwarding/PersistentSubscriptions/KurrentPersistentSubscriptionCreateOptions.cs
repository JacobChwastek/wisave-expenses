namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public sealed record KurrentPersistentSubscriptionCreateOptions(
    bool FromStartWhenCreated,
    int MaxSubscriberCount,
    string ConsumerStrategyName);

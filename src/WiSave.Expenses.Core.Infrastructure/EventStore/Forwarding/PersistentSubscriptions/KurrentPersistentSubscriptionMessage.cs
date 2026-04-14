namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public abstract record KurrentPersistentSubscriptionMessage
{
    public sealed record Confirmation(string SubscriptionId) : KurrentPersistentSubscriptionMessage;

    public sealed record Event(KurrentCommittedEvent CommittedEvent) : KurrentPersistentSubscriptionMessage;
}

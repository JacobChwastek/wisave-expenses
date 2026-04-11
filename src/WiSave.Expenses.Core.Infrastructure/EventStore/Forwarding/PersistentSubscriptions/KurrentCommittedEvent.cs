namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public sealed record KurrentCommittedEvent(
    Guid EventId,
    string EventType,
    string StreamId,
    byte[] Data,
    IKurrentSubscriptionActions Actions);

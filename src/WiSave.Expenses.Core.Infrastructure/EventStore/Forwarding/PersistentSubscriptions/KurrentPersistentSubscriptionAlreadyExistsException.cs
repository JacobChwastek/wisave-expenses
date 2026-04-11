namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public sealed class KurrentPersistentSubscriptionAlreadyExistsException(string groupName, Exception? innerException = null)
    : Exception($"Persistent subscription group '{groupName}' already exists.", innerException);

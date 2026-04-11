namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

public interface IKurrentSubscriptionActions
{
    Task AckAsync(CancellationToken ct);
    Task RetryAsync(string reason, CancellationToken ct);
    Task ParkAsync(string reason, CancellationToken ct);
    Task SkipAsync(string reason, CancellationToken ct);
}

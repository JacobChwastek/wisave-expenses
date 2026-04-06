using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Abstractions;

public interface IAggregateRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, IAggregateStream<TId>, new()
{
    Task<TAggregate?> LoadAsync(TId id, CancellationToken ct = default);
    Task SaveAsync(TAggregate aggregate, CancellationToken ct = default);
}

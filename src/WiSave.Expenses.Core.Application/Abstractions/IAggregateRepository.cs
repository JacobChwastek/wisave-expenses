using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Abstractions;

public interface IAggregateRepository<T> where T : AggregateRoot, new()
{
    Task<T?> LoadAsync(string streamId, CancellationToken ct = default);
    Task SaveAsync(T aggregate, CancellationToken ct = default);
}

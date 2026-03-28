namespace WiSave.Expenses.Core.Application.Abstractions;

public interface IBudgetUniquenessChecker
{
    Task<bool> ExistsAsync(string userId, int month, int year, CancellationToken ct = default);
    Task ReserveAsync(string budgetId, string userId, int month, int year, CancellationToken ct = default);
}

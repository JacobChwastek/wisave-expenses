namespace WiSave.Expenses.Core.Application.Abstractions;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(string categoryId, string userId, CancellationToken ct = default);
    Task<bool> SubcategoryExistsAsync(string subcategoryId, string categoryId, CancellationToken ct = default);
}

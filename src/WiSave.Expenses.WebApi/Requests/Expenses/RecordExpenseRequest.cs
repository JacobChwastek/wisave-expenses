using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.Expenses;

public sealed record RecordExpenseRequest(
    string AccountId, string CategoryId, string? SubcategoryId,
    decimal Amount, Currency Currency, DateOnly Date, string Description,
    bool Recurring = false, Dictionary<string, string>? Metadata = null);

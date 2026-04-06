using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.Expenses;

public sealed record UpdateExpenseRequest(
    decimal? Amount = null, Currency? Currency = null, DateOnly? Date = null,
    string? Description = null, string? CategoryId = null, string? SubcategoryId = null,
    bool? Recurring = null, Dictionary<string, string>? Metadata = null);

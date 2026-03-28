using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.Expenses;

public sealed record UpdateExpense(
    Guid CorrelationId,
    string UserId,
    string ExpenseId,
    decimal? Amount = null,
    Currency? Currency = null,
    DateOnly? Date = null,
    string? Description = null,
    string? CategoryId = null,
    string? SubcategoryId = null,
    bool? Recurring = null,
    Dictionary<string, string>? Metadata = null);

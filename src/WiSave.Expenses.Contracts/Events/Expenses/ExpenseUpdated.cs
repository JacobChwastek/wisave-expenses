using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.Expenses;

public sealed record ExpenseUpdated(
    string ExpenseId,
    string UserId,
    decimal? Amount,
    Currency? Currency,
    DateOnly? Date,
    string? Description,
    string? CategoryId,
    string? SubcategoryId,
    bool? Recurring,
    Dictionary<string, string>? Metadata,
    DateTimeOffset Timestamp);

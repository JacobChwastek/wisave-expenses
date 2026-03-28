using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.Expenses;

public sealed record ExpenseRecorded(
    string ExpenseId,
    string UserId,
    string AccountId,
    string CategoryId,
    string? SubcategoryId,
    decimal Amount,
    Currency Currency,
    DateOnly Date,
    string Description,
    bool Recurring,
    Dictionary<string, string>? Metadata,
    DateTimeOffset Timestamp);

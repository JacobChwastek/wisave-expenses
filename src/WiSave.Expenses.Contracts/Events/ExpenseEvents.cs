using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events;

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

public sealed record ExpenseDeleted(
    string ExpenseId,
    string UserId,
    DateTimeOffset Timestamp);

namespace WiSave.Expenses.Core.Domain.Accounting.Events;

public sealed record ExpenseRecordedEvent(
    string ExpenseId,
    string UserId,
    string AccountId,
    string CategoryId,
    string? SubcategoryId,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string Description,
    bool Recurring,
    Dictionary<string, string>? Metadata);

public sealed record ExpenseUpdatedEvent(
    string ExpenseId,
    decimal? Amount,
    string? Currency,
    DateOnly? Date,
    string? Description,
    string? CategoryId,
    string? SubcategoryId,
    bool? Recurring,
    Dictionary<string, string>? Metadata);

public sealed record ExpenseDeletedEvent(string ExpenseId);

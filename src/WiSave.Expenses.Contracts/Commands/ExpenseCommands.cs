using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands;

public sealed record RecordExpense(
    Guid CorrelationId,
    string UserId,
    string AccountId,
    string CategoryId,
    string? SubcategoryId,
    decimal Amount,
    Currency Currency,
    DateOnly Date,
    string Description,
    bool Recurring = false,
    Dictionary<string, string>? Metadata = null);

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

public sealed record DeleteExpense(
    Guid CorrelationId,
    string UserId,
    string ExpenseId);

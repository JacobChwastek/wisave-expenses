using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.Expenses;

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

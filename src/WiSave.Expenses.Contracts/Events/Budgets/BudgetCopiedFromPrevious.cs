using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.Budgets;

public sealed record BudgetCopiedFromPrevious(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    int SourceMonth,
    int SourceYear,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring,
    Dictionary<string, decimal> CategoryLimits,
    DateTimeOffset Timestamp);

using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.Budgets;

public sealed record BudgetCreated(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring,
    DateTimeOffset Timestamp);

namespace WiSave.Expenses.Contracts.Events.Budgets;

public sealed record CategoryLimitSet(
    string BudgetId,
    string UserId,
    string CategoryId,
    decimal Limit,
    DateTimeOffset Timestamp);

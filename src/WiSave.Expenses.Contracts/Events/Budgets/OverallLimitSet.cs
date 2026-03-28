namespace WiSave.Expenses.Contracts.Events.Budgets;

public sealed record OverallLimitSet(
    string BudgetId,
    string UserId,
    decimal TotalLimit,
    DateTimeOffset Timestamp);

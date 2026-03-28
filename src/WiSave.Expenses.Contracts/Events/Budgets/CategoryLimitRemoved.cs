namespace WiSave.Expenses.Contracts.Events.Budgets;

public sealed record CategoryLimitRemoved(
    string BudgetId,
    string UserId,
    string CategoryId,
    DateTimeOffset Timestamp);

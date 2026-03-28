namespace WiSave.Expenses.Contracts.Commands.Budgets;

public sealed record SetCategoryLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    string CategoryId,
    decimal Limit);

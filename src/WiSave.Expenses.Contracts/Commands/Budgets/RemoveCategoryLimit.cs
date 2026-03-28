namespace WiSave.Expenses.Contracts.Commands.Budgets;

public sealed record RemoveCategoryLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    string CategoryId);

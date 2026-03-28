namespace WiSave.Expenses.Contracts.Commands.Budgets;

public sealed record SetOverallLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    decimal TotalLimit);

namespace WiSave.Expenses.Contracts.Commands.Budgets;

public sealed record CopyBudgetFromPrevious(
    Guid CorrelationId,
    string UserId,
    int Month,
    int Year);

namespace WiSave.Expenses.Contracts.Commands.Expenses;

public sealed record DeleteExpense(
    Guid CorrelationId,
    string UserId,
    string ExpenseId);

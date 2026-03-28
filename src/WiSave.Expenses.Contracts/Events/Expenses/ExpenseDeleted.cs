namespace WiSave.Expenses.Contracts.Events.Expenses;

public sealed record ExpenseDeleted(
    string ExpenseId,
    string UserId,
    DateTimeOffset Timestamp);

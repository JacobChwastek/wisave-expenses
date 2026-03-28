namespace WiSave.Expenses.Contracts.Commands.Accounts;

public sealed record CloseAccount(
    Guid CorrelationId,
    string UserId,
    string AccountId);

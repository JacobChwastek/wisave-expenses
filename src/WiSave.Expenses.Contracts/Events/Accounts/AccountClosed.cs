namespace WiSave.Expenses.Contracts.Events.Accounts;

public sealed record AccountClosed(
    string AccountId,
    string UserId,
    DateTimeOffset Timestamp);

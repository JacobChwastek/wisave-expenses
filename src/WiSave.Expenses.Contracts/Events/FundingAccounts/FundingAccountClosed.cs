namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingAccountClosed(
    string FundingAccountId,
    string UserId,
    DateTimeOffset Timestamp);

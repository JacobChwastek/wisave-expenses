namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingTransferPosted(
    string FundingAccountId,
    string UserId,
    string TransferId,
    decimal Amount,
    DateTimeOffset PostedAtUtc,
    DateTimeOffset Timestamp);

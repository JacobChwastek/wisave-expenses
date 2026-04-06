namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingTransferPosted(
    string FundingAccountId,
    string UserId,
    string TransferId,
    string? TargetCreditCardAccountId,
    string? StatementId,
    decimal Amount,
    DateTimeOffset PostedAtUtc,
    DateTimeOffset Timestamp);

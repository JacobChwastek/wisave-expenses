namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record PostFundingTransfer(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId,
    string TransferId,
    decimal Amount,
    DateTimeOffset PostedAtUtc);

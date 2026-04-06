namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record PostFundingTransfer(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId,
    string TransferId,
    string? TargetCreditCardAccountId,
    string? StatementId,
    decimal Amount,
    DateTimeOffset PostedAtUtc);

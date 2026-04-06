namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record CloseFundingAccount(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId);

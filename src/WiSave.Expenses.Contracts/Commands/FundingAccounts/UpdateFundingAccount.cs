using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record UpdateFundingAccount(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId,
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    string? Color = null);

using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record OpenFundingAccount(
    Guid CorrelationId,
    Guid UserId,
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    decimal OpeningBalance,
    string? Color = null);

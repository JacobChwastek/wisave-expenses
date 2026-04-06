using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingAccountOpened(
    string FundingAccountId,
    string UserId,
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    decimal OpeningBalance,
    string? Color,
    DateTimeOffset Timestamp);

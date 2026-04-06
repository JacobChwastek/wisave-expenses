using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingAccountUpdated(
    string FundingAccountId,
    string UserId,
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    string? Color,
    DateTimeOffset Timestamp);

using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.Accounts;

public sealed record AccountOpened(
    string AccountId,
    string UserId,
    string Name,
    AccountType Type,
    Currency Currency,
    decimal Balance,
    string? LinkedBankAccountId,
    decimal? CreditLimit,
    int? BillingCycleDay,
    string? Color,
    string? LastFourDigits,
    DateTimeOffset Timestamp);

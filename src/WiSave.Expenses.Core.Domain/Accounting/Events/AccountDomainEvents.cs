namespace WiSave.Expenses.Core.Domain.Accounting.Events;

public sealed record AccountOpenedEvent(
    string AccountId,
    string UserId,
    string Name,
    string Type,
    string Currency,
    decimal Balance,
    string? LinkedBankAccountId,
    decimal? CreditLimit,
    int? BillingCycleDay,
    string? Color,
    string? LastFourDigits);

public sealed record AccountUpdatedEvent(
    string AccountId,
    string Name,
    string Type,
    string Currency,
    decimal Balance,
    string? LinkedBankAccountId,
    decimal? CreditLimit,
    int? BillingCycleDay,
    string? Color,
    string? LastFourDigits);

public sealed record AccountClosedEvent(string AccountId);

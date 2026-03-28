using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.Accounts;

public sealed record UpdateAccount(
    Guid CorrelationId,
    string UserId,
    string AccountId,
    string Name,
    AccountType Type,
    Currency Currency,
    decimal Balance,
    string? LinkedBankAccountId = null,
    decimal? CreditLimit = null,
    int? BillingCycleDay = null,
    string? Color = null,
    string? LastFourDigits = null);

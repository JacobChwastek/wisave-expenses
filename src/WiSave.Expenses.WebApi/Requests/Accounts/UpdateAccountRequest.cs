using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.Accounts;

public sealed record UpdateAccountRequest(
    string Name, AccountType Type, Currency Currency, decimal Balance,
    string? LinkedBankAccountId = null, decimal? CreditLimit = null, int? BillingCycleDay = null,
    string? Color = null, string? LastFourDigits = null);

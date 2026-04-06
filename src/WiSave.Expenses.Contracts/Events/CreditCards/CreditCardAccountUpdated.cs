using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.CreditCards;

public sealed record CreditCardAccountUpdated(
    string CreditCardAccountId,
    string UserId,
    string Name,
    Currency Currency,
    string SettlementAccountId,
    BankProvider BankProvider,
    string ProductCode,
    decimal CreditLimit,
    int StatementClosingDay,
    int GracePeriodDays,
    string? Color,
    string? LastFourDigits,
    DateTimeOffset Timestamp);

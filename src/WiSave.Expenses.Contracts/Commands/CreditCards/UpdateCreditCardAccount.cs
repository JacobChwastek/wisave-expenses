using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.CreditCards;

public sealed record UpdateCreditCardAccount(
    Guid CorrelationId,
    string UserId,
    string CreditCardAccountId,
    string Name,
    Currency Currency,
    string SettlementAccountId,
    BankProvider BankProvider,
    string ProductCode,
    decimal CreditLimit,
    int StatementClosingDay,
    int GracePeriodDays,
    string? Color = null,
    string? LastFourDigits = null);

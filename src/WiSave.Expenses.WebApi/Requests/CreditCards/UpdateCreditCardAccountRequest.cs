using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.CreditCards;

public sealed record UpdateCreditCardAccountRequest(
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

public static class UpdateCreditCardAccountRequestExtensions
{
    public static UpdateCreditCardAccount ToCommand(
        this UpdateCreditCardAccountRequest request,
        Guid correlationId,
        string userId,
        string creditCardAccountId)
        => new(
            correlationId,
            userId,
            creditCardAccountId,
            request.Name,
            request.Currency,
            request.SettlementAccountId,
            request.BankProvider,
            request.ProductCode,
            request.CreditLimit,
            request.StatementClosingDay,
            request.GracePeriodDays,
            request.Color,
            request.LastFourDigits);
}

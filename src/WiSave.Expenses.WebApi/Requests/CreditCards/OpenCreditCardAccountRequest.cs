using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.CreditCards;

public sealed record OpenCreditCardAccountRequest(
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

public static class OpenCreditCardAccountRequestExtensions
{
    public static OpenCreditCardAccount ToCommand(this OpenCreditCardAccountRequest request, Guid correlationId, Guid userId)
        => new(
            correlationId,
            userId,
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

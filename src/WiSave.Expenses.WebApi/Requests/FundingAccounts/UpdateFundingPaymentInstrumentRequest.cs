using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.FundingAccounts;

public sealed record UpdateFundingPaymentInstrumentRequest(
    string Name,
    PaymentInstrumentKind Kind,
    string? LastFourDigits = null,
    string? Network = null,
    string? Color = null);

public static class UpdateFundingPaymentInstrumentRequestExtensions
{
    public static UpdateFundingPaymentInstrument ToCommand(
        this UpdateFundingPaymentInstrumentRequest request,
        Guid correlationId,
        string userId,
        string fundingAccountId,
        string paymentInstrumentId)
        => new(
            correlationId,
            userId,
            fundingAccountId,
            paymentInstrumentId,
            request.Name,
            request.Kind,
            request.LastFourDigits,
            request.Network,
            request.Color);
}

using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record UpdateFundingPaymentInstrument(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId,
    string PaymentInstrumentId,
    string Name,
    PaymentInstrumentKind Kind,
    string? LastFourDigits = null,
    string? Network = null,
    string? Color = null);

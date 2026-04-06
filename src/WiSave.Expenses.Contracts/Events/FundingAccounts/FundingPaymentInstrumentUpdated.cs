using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingPaymentInstrumentUpdated(
    string FundingAccountId,
    string UserId,
    string PaymentInstrumentId,
    string Name,
    PaymentInstrumentKind Kind,
    string? LastFourDigits,
    string? Network,
    string? Color,
    DateTimeOffset Timestamp);

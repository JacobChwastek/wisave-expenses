namespace WiSave.Expenses.Contracts.Commands.FundingAccounts;

public sealed record RemoveFundingPaymentInstrument(
    Guid CorrelationId,
    string UserId,
    string FundingAccountId,
    string PaymentInstrumentId);

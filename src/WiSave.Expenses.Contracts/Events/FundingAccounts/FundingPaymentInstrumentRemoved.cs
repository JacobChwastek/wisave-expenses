namespace WiSave.Expenses.Contracts.Events.FundingAccounts;

public sealed record FundingPaymentInstrumentRemoved(
    string FundingAccountId,
    string UserId,
    string PaymentInstrumentId,
    DateTimeOffset Timestamp);

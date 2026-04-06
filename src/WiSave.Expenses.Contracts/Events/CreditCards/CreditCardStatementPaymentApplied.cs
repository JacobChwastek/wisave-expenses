namespace WiSave.Expenses.Contracts.Events.CreditCards;

public sealed record CreditCardStatementPaymentApplied(
    string CreditCardAccountId,
    string StatementId,
    string TransferId,
    decimal Amount,
    decimal StatementOutstandingBalanceAfterApplication,
    DateTimeOffset AppliedAtUtc,
    DateTimeOffset Timestamp);

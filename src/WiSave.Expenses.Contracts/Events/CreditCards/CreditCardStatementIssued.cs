namespace WiSave.Expenses.Contracts.Events.CreditCards;

public sealed record CreditCardStatementIssued(
    string CreditCardAccountId,
    string StatementId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    DateOnly StatementDate,
    DateOnly DueDate,
    decimal StatementBalance,
    decimal MinimumPaymentDue,
    decimal UnbilledBalanceAfterIssue,
    string PolicyCode,
    string PolicyVersion,
    DateTimeOffset Timestamp);

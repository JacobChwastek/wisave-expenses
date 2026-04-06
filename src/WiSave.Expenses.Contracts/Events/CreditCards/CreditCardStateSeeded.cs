namespace WiSave.Expenses.Contracts.Events.CreditCards;

public sealed record CreditCardStateSeeded(
    string CreditCardAccountId,
    string? ActiveStatementId,
    decimal ActiveStatementBalance,
    decimal ActiveStatementMinimumPaymentDue,
    DateOnly? ActiveStatementPeriodCloseDate,
    DateOnly? ActiveStatementDueDate,
    decimal UnbilledBalance,
    DateTimeOffset Timestamp);

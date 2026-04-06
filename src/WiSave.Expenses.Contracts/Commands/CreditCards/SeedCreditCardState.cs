namespace WiSave.Expenses.Contracts.Commands.CreditCards;

public sealed record SeedCreditCardState(
    Guid CorrelationId,
    string UserId,
    string CreditCardAccountId,
    decimal ActiveStatementBalance,
    decimal ActiveStatementMinimumPaymentDue,
    DateOnly? ActiveStatementPeriodCloseDate,
    DateOnly? ActiveStatementDueDate,
    decimal UnbilledBalance);

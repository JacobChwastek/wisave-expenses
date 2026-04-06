namespace WiSave.Expenses.Contracts.Commands.CreditCards;

public sealed record IssueCreditCardStatement(
    Guid CorrelationId,
    string UserId,
    string CreditCardAccountId,
    DateOnly CalculationDate);

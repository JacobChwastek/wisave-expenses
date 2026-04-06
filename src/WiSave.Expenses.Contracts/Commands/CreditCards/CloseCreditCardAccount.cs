namespace WiSave.Expenses.Contracts.Commands.CreditCards;

public sealed record CloseCreditCardAccount(
    Guid CorrelationId,
    string UserId,
    string CreditCardAccountId);

namespace WiSave.Expenses.Contracts.Events.CreditCards;

public sealed record CreditCardAccountClosed(
    string CreditCardAccountId,
    string UserId,
    DateTimeOffset Timestamp);

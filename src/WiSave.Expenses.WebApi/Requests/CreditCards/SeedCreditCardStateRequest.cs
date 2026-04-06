using WiSave.Expenses.Contracts.Commands.CreditCards;

namespace WiSave.Expenses.WebApi.Requests.CreditCards;

public sealed record SeedCreditCardStateRequest(
    decimal ActiveStatementBalance,
    decimal ActiveStatementMinimumPaymentDue,
    DateOnly? ActiveStatementPeriodCloseDate,
    DateOnly? ActiveStatementDueDate,
    decimal UnbilledBalance);

public static class SeedCreditCardStateRequestExtensions
{
    public static SeedCreditCardState ToCommand(
        this SeedCreditCardStateRequest request,
        Guid correlationId,
        string userId,
        string creditCardAccountId)
        => new(
            correlationId,
            userId,
            creditCardAccountId,
            request.ActiveStatementBalance,
            request.ActiveStatementMinimumPaymentDue,
            request.ActiveStatementPeriodCloseDate,
            request.ActiveStatementDueDate,
            request.UnbilledBalance);
}

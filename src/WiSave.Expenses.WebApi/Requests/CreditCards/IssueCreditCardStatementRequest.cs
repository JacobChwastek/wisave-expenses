using WiSave.Expenses.Contracts.Commands.CreditCards;

namespace WiSave.Expenses.WebApi.Requests.CreditCards;

public sealed record IssueCreditCardStatementRequest(DateOnly CalculationDate);

public static class IssueCreditCardStatementRequestExtensions
{
    public static IssueCreditCardStatement ToCommand(
        this IssueCreditCardStatementRequest request,
        Guid correlationId,
        string userId,
        string creditCardAccountId)
        => new(correlationId, userId, creditCardAccountId, request.CalculationDate);
}

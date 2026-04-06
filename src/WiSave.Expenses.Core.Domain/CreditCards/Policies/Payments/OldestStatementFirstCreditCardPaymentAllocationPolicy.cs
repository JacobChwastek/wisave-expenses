using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;

/// <summary>
/// Allocates settlement payments to the oldest due open statements first.
/// </summary>
/// <remarks>
/// The policy is intentionally stateless and deterministic. It never allocates
/// more than a statement outstanding balance and stops when the transfer amount
/// is exhausted.
/// </remarks>
public sealed class OldestStatementFirstCreditCardPaymentAllocationPolicy : ICreditCardPaymentAllocationPolicy
{
    /// <inheritdoc />
    public IReadOnlyCollection<CreditCardPaymentAllocationDecision> Allocate(
        decimal paymentAmount,
        IReadOnlyCollection<OpenStatementSnapshot> openStatements)
    {
        if (paymentAmount <= 0m)
            throw new PaymentAmountMustBeGreaterThanZeroException();

        var remaining = paymentAmount;
        var decisions = new List<CreditCardPaymentAllocationDecision>();

        foreach (var statement in openStatements.OrderBy(x => x.DueDate))
        {
            if (remaining <= 0m)
                break;

            var applied = Math.Min(remaining, statement.OutstandingBalance);
            if (applied <= 0m)
                continue;

            decisions.Add(new CreditCardPaymentAllocationDecision(statement.StatementId, applied));
            remaining -= applied;
        }

        return decisions;
    }
}

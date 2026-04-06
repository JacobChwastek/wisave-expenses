using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

namespace WiSave.Expenses.Core.Domain.CreditCards.Specifications;

/// <summary>
/// Checks whether an existing statement exactly matches a newly computed statement proposal.
/// </summary>
/// <remarks>
/// The aggregate uses this specification to make statement issuance idempotent.
/// A duplicate issue command is accepted only when all persisted statement values
/// match the recomputed values.
/// </remarks>
public sealed class StatementMatchesComputationSpecification(CreditCardStatementComputation computation)
{
    /// <summary>
    /// Evaluates whether the statement matches the computation supplied to this specification.
    /// </summary>
    /// <param name="statement">Existing statement from aggregate state.</param>
    /// <returns><c>true</c> when the statement is equivalent to the computation.</returns>
    public bool IsSatisfiedBy(CreditCardStatement statement) =>
        statement.PeriodFrom == computation.PeriodFrom
        && statement.PeriodTo == computation.PeriodTo
        && statement.StatementDate == computation.StatementDate
        && statement.DueDate == computation.DueDate
        && statement.StatementBalance == computation.StatementBalance
        && statement.MinimumPaymentDue == computation.MinimumPaymentDue
        && statement.UnbilledBalanceAfterIssue == computation.UnbilledBalanceAfterIssue
        && statement.PolicyCode == computation.PolicyCode
        && statement.PolicyVersion == computation.PolicyVersion;
}

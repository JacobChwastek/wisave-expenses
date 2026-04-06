namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;

/// <summary>
/// Builds statement-level payment allocations for a credit-card settlement transfer.
/// </summary>
public interface ICreditCardPaymentAllocationPolicy
{
    /// <summary>
    /// Splits a transfer amount across the supplied open statements.
    /// </summary>
    /// <param name="paymentAmount">Total settlement amount available for allocation.</param>
    /// <param name="openStatements">Statements that still have outstanding balance.</param>
    /// <returns>Allocation decisions that the aggregate will validate and apply.</returns>
    IReadOnlyCollection<CreditCardPaymentAllocationDecision> Allocate(
        decimal paymentAmount,
        IReadOnlyCollection<OpenStatementSnapshot> openStatements);
}

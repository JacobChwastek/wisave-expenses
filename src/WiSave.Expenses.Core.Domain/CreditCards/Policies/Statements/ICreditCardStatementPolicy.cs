namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// Computes statement dates and amounts for a credit-card product.
/// </summary>
public interface ICreditCardStatementPolicy
{
    /// <summary>
    /// Computes the statement proposal for the supplied account context.
    /// </summary>
    /// <param name="context">Validated account and calculation inputs.</param>
    /// <returns>The deterministic statement computation to apply to the aggregate.</returns>
    CreditCardStatementComputation Compute(CreditCardStatementPolicyContext context);
}

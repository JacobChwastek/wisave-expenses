using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// Resolves the statement policy for a bank provider and product code.
/// </summary>
public interface ICreditCardStatementPolicyResolver
{
    /// <summary>
    /// Returns the policy that owns statement computation for the provider/product pair.
    /// </summary>
    /// <param name="bankProvider">Bank provider configured on the card.</param>
    /// <param name="productCode">Provider-specific card product code.</param>
    /// <returns>The matching statement policy.</returns>
    ICreditCardStatementPolicy Resolve(BankProvider bankProvider, string productCode);
}

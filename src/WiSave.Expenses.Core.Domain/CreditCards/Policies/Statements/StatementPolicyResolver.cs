using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// Default resolver for credit-card statement policies known by the domain.
/// </summary>
/// <remarks>
/// Keep this resolver explicit so unsupported provider/product combinations fail
/// before statement issuance mutates the aggregate.
/// </remarks>
public sealed class StatementPolicyResolver : ICreditCardStatementPolicyResolver
{
    private readonly MBankStatementPolicy _mBankStatementPolicy = new();

    /// <inheritdoc />
    public ICreditCardStatementPolicy Resolve(BankProvider bankProvider, string productCode)
    {
        if (bankProvider == BankProvider.MBank && string.Equals(productCode, "STANDARD", StringComparison.OrdinalIgnoreCase))
            return _mBankStatementPolicy;

        throw new UnsupportedCreditCardStatementPolicyException();
    }
}

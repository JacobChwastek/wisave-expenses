using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// Input data required to compute a credit-card statement for a specific account.
/// </summary>
/// <remarks>
/// Policy implementations should use only this context and deterministic rules.
/// External reads, clocks, and mutable dependencies belong outside policy
/// calculation so statement issuance remains replay-friendly.
/// </remarks>
public sealed record CreditCardStatementPolicyContext(
    /// <summary>Credit-card account being evaluated.</summary>
    CreditCardAccountId AccountId,
    /// <summary>Currency of the account and statement amounts.</summary>
    Currency Currency,
    /// <summary>Configured credit limit at calculation time.</summary>
    decimal CreditLimit,
    /// <summary>Current unbilled balance available to close into a statement.</summary>
    decimal CurrentUnbilledBalance,
    /// <summary>Configured monthly statement closing day.</summary>
    StatementClosingDay StatementClosingDay,
    /// <summary>Configured number of grace-period days after statement issue.</summary>
    GracePeriodDays GracePeriodDays,
    /// <summary>Business date for which statement issuance is being attempted.</summary>
    DateOnly CalculationDate);

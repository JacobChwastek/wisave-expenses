namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// Represents the statement data produced by a bank-specific statement policy.
/// </summary>
/// <remarks>
/// The aggregate treats this as a deterministic proposal for the next statement.
/// Reissuing the same statement is idempotent only when these values match the
/// already recorded statement values.
/// </remarks>
public sealed record CreditCardStatementComputation(
    /// <summary>First day included in the statement period.</summary>
    DateOnly PeriodFrom,
    /// <summary>Last day included in the statement period.</summary>
    DateOnly PeriodTo,
    /// <summary>Date on which the statement is issued.</summary>
    DateOnly StatementDate,
    /// <summary>Date by which the statement should be paid.</summary>
    DateOnly DueDate,
    /// <summary>Total amount moved from unbilled balance into the statement.</summary>
    decimal StatementBalance,
    /// <summary>Minimum amount required by the statement policy.</summary>
    decimal MinimumPaymentDue,
    /// <summary>Remaining unbilled balance after the statement is issued.</summary>
    decimal UnbilledBalanceAfterIssue,
    /// <summary>Stable policy code used for audit and replay diagnostics.</summary>
    string PolicyCode,
    /// <summary>Stable policy version used to identify the calculation rules.</summary>
    string PolicyVersion);

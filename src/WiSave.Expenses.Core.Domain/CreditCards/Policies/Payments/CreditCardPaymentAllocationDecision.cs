namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;

/// <summary>
/// Describes how much of a settlement transfer should be applied to one statement.
/// </summary>
/// <param name="StatementId">Statement receiving the payment application.</param>
/// <param name="Amount">Amount to apply to the statement outstanding balance.</param>
public sealed record CreditCardPaymentAllocationDecision(
    string StatementId,
    decimal Amount);

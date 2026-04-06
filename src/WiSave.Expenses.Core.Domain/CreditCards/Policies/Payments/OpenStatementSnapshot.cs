namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;

/// <summary>
/// Read-only snapshot of a statement that can still receive settlement payments.
/// </summary>
/// <param name="StatementId">Identifier of the open statement.</param>
/// <param name="DueDate">Due date used to order allocations.</param>
/// <param name="OutstandingBalance">Remaining statement balance available for payment.</param>
public sealed record OpenStatementSnapshot(
    string StatementId,
    DateOnly DueDate,
    decimal OutstandingBalance);

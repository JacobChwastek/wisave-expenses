using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;

public sealed record StatementPaymentApplication
{
    public TransferId TransferId { get; }
    public StatementPaymentAmount Amount { get; }
    public DateTimeOffset AppliedAtUtc { get; }

    public StatementPaymentApplication(
        TransferId transferId,
        decimal amount,
        DateTimeOffset appliedAtUtc)
        : this(transferId, new StatementPaymentAmount(amount), appliedAtUtc)
    {
    }

    public StatementPaymentApplication(
        TransferId transferId,
        StatementPaymentAmount amount,
        DateTimeOffset appliedAtUtc)
    {
        TransferId = transferId;
        Amount = amount;
        AppliedAtUtc = appliedAtUtc;
    }
}

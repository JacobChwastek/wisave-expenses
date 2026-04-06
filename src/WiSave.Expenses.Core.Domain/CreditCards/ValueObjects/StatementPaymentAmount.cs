using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;

public sealed record StatementPaymentAmount
{
    public decimal Value { get; }

    public StatementPaymentAmount(decimal value)
    {
        if (value <= 0m)
            throw new PaymentApplicationAmountMustBeGreaterThanZeroException();

        Value = value;
    }

    public static explicit operator decimal(StatementPaymentAmount amount) => amount.Value;
    public static explicit operator StatementPaymentAmount(decimal value) => new(value);
}

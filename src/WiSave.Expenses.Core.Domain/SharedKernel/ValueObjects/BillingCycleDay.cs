namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record BillingCycleDay
{
    public int Value { get; }

    public BillingCycleDay(int value)
    {
        if (value is < 1 or > 31)
            throw new DomainException("Billing cycle day must be between 1 and 31.");

        Value = value;
    }

    public static explicit operator int(BillingCycleDay day) => day.Value;
    public static explicit operator BillingCycleDay(int value) => new(value);
}

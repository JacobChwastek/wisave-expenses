namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record BillingCycleDay
{
    public int Day { get; }

    public BillingCycleDay(int day)
    {
        if (day is < 1 or > 31)
            throw new DomainException("Billing cycle day must be between 1 and 31.");

        Day = day;
    }
}

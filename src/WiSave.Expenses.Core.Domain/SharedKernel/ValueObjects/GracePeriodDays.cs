using WiSave.Framework.Domain;

namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record GracePeriodDays
{
    public int Value { get; }

    public GracePeriodDays(int value)
    {
        if (value is < 1 or > 60)
            throw new DomainException("Grace period days must be between 1 and 60.");

        Value = value;
    }

    public static explicit operator int(GracePeriodDays days) => days.Value;
    public static explicit operator GracePeriodDays(int value) => new(value);
}

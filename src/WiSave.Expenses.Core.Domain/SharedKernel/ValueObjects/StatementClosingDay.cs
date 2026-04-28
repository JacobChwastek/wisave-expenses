using WiSave.Framework.Domain;

namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record StatementClosingDay
{
    public int Value { get; }

    public StatementClosingDay(int value)
    {
        if (value is < 1 or > 31)
            throw new DomainException("Statement closing day must be between 1 and 31.");

        Value = value;
    }

    public static explicit operator int(StatementClosingDay day) => day.Value;
    public static explicit operator StatementClosingDay(int value) => new(value);
}

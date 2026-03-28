namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record BudgetPeriod
{
    public int Month { get; }
    public int Year { get; }

    public BudgetPeriod(int month, int year)
    {
        if (month is < 1 or > 12)
            throw new DomainException("Month must be between 1 and 12.");
        if (year < 2000)
            throw new DomainException("Year must be >= 2000.");

        Month = month;
        Year = year;
    }

    public BudgetPeriod Previous() =>
        Month == 1 ? new BudgetPeriod(12, Year - 1) : new BudgetPeriod(Month - 1, Year);

    public override string ToString() => $"{Month:D2}/{Year}";
}

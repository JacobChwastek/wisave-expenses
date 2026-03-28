namespace WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

public sealed record CategoryBudget
{
    public string CategoryId { get; }
    public decimal Limit { get; }

    public CategoryBudget(string categoryId, decimal limit)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new DomainException("Category ID is required.");
        if (limit < 0)
            throw new DomainException("Category limit must be >= 0.");

        CategoryId = categoryId;
        Limit = limit;
    }
}

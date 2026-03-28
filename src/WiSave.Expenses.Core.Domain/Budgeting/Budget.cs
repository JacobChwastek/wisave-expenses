using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Budgeting;

public sealed class Budget : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal TotalLimit { get; private set; }
    public Currency Currency { get; private set; }
    public bool Recurring { get; private set; } = true;

    private readonly Dictionary<string, decimal> _categoryLimits = [];
    public IReadOnlyDictionary<string, decimal> CategoryLimits => _categoryLimits;

    public Budget() { }

    public static Budget Create(
        string id, string userId, int month, int year, decimal totalLimit, Currency currency, bool recurring = true)
    {
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");
        if (month is < 1 or > 12)
            throw new DomainException("Month must be between 1 and 12.");

        var budget = new Budget();
        budget.RaiseEvent(new BudgetCreated(id, userId, month, year, totalLimit, currency, recurring, DateTimeOffset.UtcNow));
        return budget;
    }

    public static Budget CopyFromPrevious(
        string id, string userId, int month, int year, int sourceMonth, int sourceYear,
        Currency currency, decimal totalLimit, bool recurring, IReadOnlyDictionary<string, decimal> categoryLimits)
    {
        var budget = new Budget();
        budget.RaiseEvent(new BudgetCopiedFromPrevious(
            id, userId, month, year, sourceMonth, sourceYear, DateTimeOffset.UtcNow));

        // Apply source state that the event doesn't carry
        budget.Currency = currency;
        budget.TotalLimit = totalLimit;
        budget.Recurring = recurring;
        foreach (var (catId, limit) in categoryLimits)
            budget._categoryLimits[catId] = limit;

        return budget;
    }

    public void SetOverallLimit(decimal totalLimit)
    {
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");

        RaiseEvent(new OverallLimitSet(Id, UserId, totalLimit, DateTimeOffset.UtcNow));
    }

    public void SetCategoryLimit(string categoryId, decimal limit)
    {
        if (limit < 0)
            throw new DomainException("Category limit must be >= 0.");

        RaiseEvent(new CategoryLimitSet(Id, UserId, categoryId, limit, DateTimeOffset.UtcNow));
    }

    public void RemoveCategoryLimit(string categoryId)
    {
        if (!_categoryLimits.ContainsKey(categoryId))
            throw new DomainException($"Category '{categoryId}' does not have a budget.");

        RaiseEvent(new CategoryLimitRemoved(Id, UserId, categoryId, DateTimeOffset.UtcNow));
    }

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case BudgetCreated e:
                Id = e.BudgetId;
                UserId = e.UserId;
                Month = e.Month;
                Year = e.Year;
                TotalLimit = e.TotalLimit;
                Currency = e.Currency;
                Recurring = e.Recurring;
                break;

            case BudgetCopiedFromPrevious e:
                Id = e.BudgetId;
                UserId = e.UserId;
                Month = e.Month;
                Year = e.Year;
                break;

            case OverallLimitSet e:
                TotalLimit = e.TotalLimit;
                break;

            case CategoryLimitSet e:
                _categoryLimits[e.CategoryId] = e.Limit;
                break;

            case CategoryLimitRemoved e:
                _categoryLimits.Remove(e.CategoryId);
                break;
        }
    }
}

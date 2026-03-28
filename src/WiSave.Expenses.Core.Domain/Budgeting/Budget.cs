using WiSave.Expenses.Core.Domain.Budgeting.Events;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Budgeting;

public sealed class Budget : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal TotalLimit { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool Recurring { get; private set; } = true;

    private readonly Dictionary<string, decimal> _categoryLimits = [];
    public IReadOnlyDictionary<string, decimal> CategoryLimits => _categoryLimits;

    public Budget() { }

    public static Budget Create(
        string id, string userId, int month, int year, decimal totalLimit, string currency, bool recurring = true)
    {
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");
        if (month is < 1 or > 12)
            throw new DomainException("Month must be between 1 and 12.");

        var budget = new Budget();
        budget.RaiseEvent(new BudgetCreatedEvent(id, userId, month, year, totalLimit, currency, recurring));
        return budget;
    }

    public static Budget CopyFromPrevious(
        string id, string userId, int month, int year, int sourceMonth, int sourceYear,
        string currency, decimal totalLimit, bool recurring, IReadOnlyDictionary<string, decimal> categoryLimits)
    {
        var budget = new Budget();
        budget.RaiseEvent(new BudgetCopiedFromPreviousEvent(
            id, userId, month, year, sourceMonth, sourceYear,
            categoryLimits.Select(kv => new CategoryBudgetSnapshot(kv.Key, kv.Value)).ToList()));
        // Restore fields that CopyFromPrevious carries from the source
        budget.Currency = currency;
        budget.TotalLimit = totalLimit;
        budget.Recurring = recurring;
        return budget;
    }

    public void SetOverallLimit(decimal totalLimit)
    {
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");

        RaiseEvent(new OverallLimitSetEvent(Id, totalLimit));
    }

    public void SetCategoryLimit(string categoryId, decimal limit)
    {
        if (limit < 0)
            throw new DomainException("Category limit must be >= 0.");

        RaiseEvent(new CategoryLimitSetEvent(Id, categoryId, limit));
    }

    public void RemoveCategoryLimit(string categoryId)
    {
        if (!_categoryLimits.ContainsKey(categoryId))
            throw new DomainException($"Category '{categoryId}' does not have a budget.");

        RaiseEvent(new CategoryLimitRemovedEvent(Id, categoryId));
    }

    public void ToggleRecurring(bool recurring)
    {
        RaiseEvent(new RecurringToggledEvent(Id, recurring));
    }

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case BudgetCreatedEvent e:
                Id = e.BudgetId;
                UserId = e.UserId;
                Month = e.Month;
                Year = e.Year;
                TotalLimit = e.TotalLimit;
                Currency = e.Currency;
                Recurring = e.Recurring;
                break;

            case BudgetCopiedFromPreviousEvent e:
                Id = e.BudgetId;
                UserId = e.UserId;
                Month = e.Month;
                Year = e.Year;
                foreach (var cb in e.CategoryBudgets)
                    _categoryLimits[cb.CategoryId] = cb.Limit;
                break;

            case OverallLimitSetEvent e:
                TotalLimit = e.TotalLimit;
                break;

            case CategoryLimitSetEvent e:
                _categoryLimits[e.CategoryId] = e.Limit;
                break;

            case CategoryLimitRemovedEvent e:
                _categoryLimits.Remove(e.CategoryId);
                break;

            case RecurringToggledEvent e:
                Recurring = e.Recurring;
                break;
        }
    }
}

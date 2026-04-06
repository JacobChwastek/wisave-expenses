using WiSave.Expenses.Contracts.Events.Budgets;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.Budgeting;

public sealed class Budget : AggregateRoot<BudgetId>, IAggregateStream<BudgetId>
{
    public UserId UserId { get; private set; } = null!;
    public BudgetPeriod Period { get; private set; } = null!;
    public decimal TotalLimit { get; private set; }
    public Currency Currency { get; private set; }
    public bool Recurring { get; private set; } = true;

    private readonly List<CategoryBudget> _categoryBudgets = [];
    public IReadOnlyList<CategoryBudget> CategoryBudgets => _categoryBudgets.AsReadOnly();

    public static string ToStreamId(BudgetId id) => $"budget-{id.Value}";

    public Budget() { }

    public static Budget Create(
        BudgetId id, UserId userId, int month, int year, decimal totalLimit, Currency currency, bool recurring = true)
    {
        _ = new BudgetPeriod(month, year);
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");

        var budget = new Budget();
        budget.RaiseEvent(new BudgetCreated(id.Value, userId.Value, month, year, totalLimit, currency, recurring, DateTimeOffset.UtcNow));
        return budget;
    }

    public static Budget CopyFromPrevious(
        BudgetId id, UserId userId, int month, int year, int sourceMonth, int sourceYear,
        Currency currency, decimal totalLimit, bool recurring, IReadOnlyDictionary<string, decimal> categoryLimits)
    {
        _ = new BudgetPeriod(month, year);
        _ = new BudgetPeriod(sourceMonth, sourceYear);

        var budget = new Budget();
        budget.RaiseEvent(new BudgetCopiedFromPrevious(
            id.Value, userId.Value, month, year, sourceMonth, sourceYear,
            totalLimit, currency, recurring,
            new Dictionary<string, decimal>(categoryLimits),
            DateTimeOffset.UtcNow));
        return budget;
    }

    public void SetOverallLimit(decimal totalLimit)
    {
        if (totalLimit < 0)
            throw new DomainException("Total limit must be >= 0.");

        RaiseEvent(new OverallLimitSet(Id.Value, UserId.Value, totalLimit, DateTimeOffset.UtcNow));
    }

    public void SetCategoryLimit(CategoryId categoryId, decimal limit)
    {
        _ = new CategoryBudget(categoryId.Value, limit);
        RaiseEvent(new CategoryLimitSet(Id.Value, UserId.Value, categoryId.Value, limit, DateTimeOffset.UtcNow));
    }

    public void RemoveCategoryLimit(CategoryId categoryId)
    {
        if (_categoryBudgets.All(cb => cb.CategoryId != categoryId.Value))
            throw new DomainException($"Category '{categoryId.Value}' does not have a budget.");

        RaiseEvent(new CategoryLimitRemoved(Id.Value, UserId.Value, categoryId.Value, DateTimeOffset.UtcNow));
    }

    #region Apply

    public void Apply(BudgetCreated e)
    {
        Id = new BudgetId(e.BudgetId);
        UserId = new UserId(e.UserId);
        Period = new BudgetPeriod(e.Month, e.Year);
        TotalLimit = e.TotalLimit;
        Currency = e.Currency;
        Recurring = e.Recurring;
    }

    public void Apply(BudgetCopiedFromPrevious e)
    {
        Id = new BudgetId(e.BudgetId);
        UserId = new UserId(e.UserId);
        Period = new BudgetPeriod(e.Month, e.Year);
        TotalLimit = e.TotalLimit;
        Currency = e.Currency;
        Recurring = e.Recurring;
        _categoryBudgets.Clear();
        foreach (var (catId, limit) in e.CategoryLimits)
            _categoryBudgets.Add(new CategoryBudget(catId, limit));
    }

    public void Apply(OverallLimitSet e)
    {
        TotalLimit = e.TotalLimit;
    }

    public void Apply(CategoryLimitSet e)
    {
        var existing = _categoryBudgets.FindIndex(cb => cb.CategoryId == e.CategoryId);
        var newCb = new CategoryBudget(e.CategoryId, e.Limit);
        if (existing >= 0)
            _categoryBudgets[existing] = newCb;
        else
            _categoryBudgets.Add(newCb);
    }

    public void Apply(CategoryLimitRemoved e)
    {
        _categoryBudgets.RemoveAll(cb => cb.CategoryId == e.CategoryId);
    }

    #endregion
}

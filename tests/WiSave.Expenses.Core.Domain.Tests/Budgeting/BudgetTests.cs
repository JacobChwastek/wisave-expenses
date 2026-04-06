using WiSave.Expenses.Contracts.Events.Budgets;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Budgeting;

public class BudgetTests
{
    private static readonly BudgetId Id = new("bud-1");
    private static readonly UserId User = new("user-1");
    private static readonly CategoryId Cat1 = new("cat-1");
    private static readonly CategoryId Cat2 = new("cat-2");

    [Fact]
    public void Create_sets_initial_state()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);

        Assert.Equal(Id, budget.Id);
        Assert.Equal(3, budget.Period.Month);
        Assert.Equal(2026, budget.Period.Year);
        Assert.Equal(8000m, budget.TotalLimit);
        Assert.Equal(Currency.PLN, budget.Currency);
        Assert.True(budget.Recurring);
        Assert.Empty(budget.CategoryBudgets);
        Assert.Single(budget.GetUncommittedEvents());
        Assert.IsType<BudgetCreated>(budget.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Given_budget_id_When_building_stream_id_Then_budget_stream_prefix_is_used()
    {
        Assert.Equal("budget-bud-1", Budget.ToStreamId(Id));
    }

    [Fact]
    public void Create_rejects_negative_limit()
    {
        Assert.Throws<DomainException>(() =>
            Budget.Create(Id, User, 3, 2026, -100m, Currency.PLN));
    }

    [Fact]
    public void Create_rejects_invalid_month()
    {
        Assert.Throws<DomainException>(() =>
            Budget.Create(Id, User, 13, 2026, 8000m, Currency.PLN));
    }

    [Fact]
    public void SetOverallLimit_updates_limit()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);
        budget.SetOverallLimit(10000m);

        Assert.Equal(10000m, budget.TotalLimit);
    }

    [Fact]
    public void SetOverallLimit_rejects_negative()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.SetOverallLimit(-1m));
    }

    [Fact]
    public void SetCategoryLimit_adds_category()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit(Cat1, 2000m);

        Assert.Single(budget.CategoryBudgets);
        Assert.Equal("cat-1", budget.CategoryBudgets[0].CategoryId);
        Assert.Equal(2000m, budget.CategoryBudgets[0].Limit);
    }

    [Fact]
    public void SetCategoryLimit_updates_existing()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit(Cat1, 2000m);
        budget.SetCategoryLimit(Cat1, 3000m);

        Assert.Single(budget.CategoryBudgets);
        Assert.Equal(3000m, budget.CategoryBudgets[0].Limit);
    }

    [Fact]
    public void SetCategoryLimit_rejects_negative()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.SetCategoryLimit(Cat1, -100m));
    }

    [Fact]
    public void RemoveCategoryLimit_removes_existing()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit(Cat1, 2000m);
        budget.RemoveCategoryLimit(Cat1);

        Assert.Empty(budget.CategoryBudgets);
    }

    [Fact]
    public void RemoveCategoryLimit_throws_for_nonexistent()
    {
        var budget = Budget.Create(Id, User, 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.RemoveCategoryLimit(new CategoryId("cat-999")));
    }

    [Fact]
    public void CopyFromPrevious_creates_with_source_categories()
    {
        var sourceLimits = new Dictionary<string, decimal>
        {
            ["cat-1"] = 3000m,
            ["cat-2"] = 2000m,
        };

        var budget = Budget.CopyFromPrevious(
            new BudgetId("bud-2"), User, 4, 2026, 3, 2026, Currency.PLN, 8000m, true, sourceLimits);

        Assert.Equal(new BudgetId("bud-2"), budget.Id);
        Assert.Equal(4, budget.Period.Month);
        Assert.Equal(8000m, budget.TotalLimit);
        Assert.Equal(2, budget.CategoryBudgets.Count);
        Assert.Equal(3000m, budget.CategoryBudgets.First(cb => cb.CategoryId == "cat-1").Limit);
    }

    [Fact]
    public void CopyFromPrevious_replays_correctly()
    {
        var sourceLimits = new Dictionary<string, decimal> { ["cat-1"] = 3000m };
        var original = Budget.CopyFromPrevious(
            new BudgetId("bud-2"), User, 4, 2026, 3, 2026, Currency.PLN, 8000m, true, sourceLimits);
        var events = original.GetUncommittedEvents();

        var replayed = new Budget();
        replayed.ReplayEvents(events);

        Assert.Equal(new BudgetId("bud-2"), replayed.Id);
        Assert.Equal(4, replayed.Period.Month);
        Assert.Equal(8000m, replayed.TotalLimit);
        Assert.Equal(Currency.PLN, replayed.Currency);
        Assert.True(replayed.Recurring);
        Assert.Single(replayed.CategoryBudgets);
        Assert.Equal(3000m, replayed.CategoryBudgets[0].Limit);
    }
}

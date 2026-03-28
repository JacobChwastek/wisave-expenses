using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Budgeting;

public class BudgetTests
{
    [Fact]
    public void Create_sets_initial_state()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);

        Assert.Equal("bud-1", budget.Id);
        Assert.Equal(3, budget.Month);
        Assert.Equal(2026, budget.Year);
        Assert.Equal(8000m, budget.TotalLimit);
        Assert.True(budget.Recurring);
        Assert.Empty(budget.CategoryLimits);
        Assert.Single(budget.GetUncommittedEvents());
        Assert.IsType<BudgetCreated>(budget.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Create_rejects_negative_limit()
    {
        Assert.Throws<DomainException>(() =>
            Budget.Create("bud-1", "user-1", 3, 2026, -100m, Currency.PLN));
    }

    [Fact]
    public void Create_rejects_invalid_month()
    {
        Assert.Throws<DomainException>(() =>
            Budget.Create("bud-1", "user-1", 13, 2026, 8000m, Currency.PLN));
    }

    [Fact]
    public void SetOverallLimit_updates_limit()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);
        budget.SetOverallLimit(10000m);

        Assert.Equal(10000m, budget.TotalLimit);
    }

    [Fact]
    public void SetOverallLimit_rejects_negative()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.SetOverallLimit(-1m));
    }

    [Fact]
    public void SetCategoryLimit_adds_category()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit("cat-1", 2000m);

        Assert.Single(budget.CategoryLimits);
        Assert.Equal(2000m, budget.CategoryLimits["cat-1"]);
    }

    [Fact]
    public void SetCategoryLimit_updates_existing()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit("cat-1", 2000m);
        budget.SetCategoryLimit("cat-1", 3000m);

        Assert.Equal(3000m, budget.CategoryLimits["cat-1"]);
    }

    [Fact]
    public void SetCategoryLimit_rejects_negative()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.SetCategoryLimit("cat-1", -100m));
    }

    [Fact]
    public void RemoveCategoryLimit_removes_existing()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);
        budget.SetCategoryLimit("cat-1", 2000m);
        budget.RemoveCategoryLimit("cat-1");

        Assert.Empty(budget.CategoryLimits);
    }

    [Fact]
    public void RemoveCategoryLimit_throws_for_nonexistent()
    {
        var budget = Budget.Create("bud-1", "user-1", 3, 2026, 8000m, Currency.PLN);

        Assert.Throws<DomainException>(() => budget.RemoveCategoryLimit("cat-999"));
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
            "bud-2", "user-1", 4, 2026, 3, 2026, Currency.PLN, 8000m, true, sourceLimits);

        Assert.Equal("bud-2", budget.Id);
        Assert.Equal(4, budget.Month);
        Assert.Equal(2, budget.CategoryLimits.Count);
        Assert.Equal(3000m, budget.CategoryLimits["cat-1"]);
    }
}

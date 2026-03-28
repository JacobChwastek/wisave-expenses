using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Accounting;

public class ExpenseTests
{
    [Fact]
    public void Record_creates_expense_with_correct_state()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            285.50m, Currency.PLN, new DateOnly(2026, 3, 25), "Weekly groceries");

        Assert.Equal("exp-1", expense.Id);
        Assert.Equal(285.50m, expense.Amount);
        Assert.False(expense.IsDeleted);
        Assert.Single(expense.GetUncommittedEvents());
        Assert.IsType<ExpenseRecorded>(expense.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Record_rejects_zero_amount()
    {
        Assert.Throws<DomainException>(() =>
            Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
                0m, Currency.PLN, new DateOnly(2026, 3, 25), "Bad"));
    }

    [Fact]
    public void Record_rejects_negative_amount()
    {
        Assert.Throws<DomainException>(() =>
            Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
                -10m, Currency.PLN, new DateOnly(2026, 3, 25), "Bad"));
    }

    [Fact]
    public void Update_modifies_fields()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.ClearUncommittedEvents();

        expense.Update(amount: 150m, description: "Lunch");

        Assert.Equal(150m, expense.Amount);
        Assert.Equal("Lunch", expense.Description);
    }

    [Fact]
    public void Update_rejects_negative_amount()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");

        Assert.Throws<DomainException>(() => expense.Update(amount: -5m));
    }

    [Fact]
    public void Delete_marks_as_deleted()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.True(expense.IsDeleted);
    }

    [Fact]
    public void Cannot_update_deleted_expense()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.Throws<DomainException>(() => expense.Update(amount: 200m));
    }

    [Fact]
    public void Cannot_delete_already_deleted_expense()
    {
        var expense = Expense.Record("exp-1", "user-1", "acc-1", "cat-1", null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.Throws<DomainException>(() => expense.Delete());
    }
}

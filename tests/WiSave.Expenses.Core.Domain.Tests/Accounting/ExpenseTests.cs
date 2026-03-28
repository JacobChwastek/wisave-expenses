using WiSave.Expenses.Contracts.Events.Expenses;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Accounting;

public class ExpenseTests
{
    private static readonly ExpenseId Id = new("exp-1");
    private static readonly UserId User = new("user-1");
    private static readonly AccountId Account = new("acc-1");
    private static readonly CategoryId Category = new("cat-1");

    [Fact]
    public void Record_creates_expense_with_correct_state()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            285.50m, Currency.PLN, new DateOnly(2026, 3, 25), "Weekly groceries");

        Assert.Equal("exp-1", expense.Id);
        Assert.Equal(285.50m, expense.Amount);
        Assert.Equal(Currency.PLN, expense.Currency);
        Assert.False(expense.IsDeleted);
        Assert.Single(expense.GetUncommittedEvents());
        Assert.IsType<ExpenseRecorded>(expense.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Record_rejects_zero_amount()
    {
        Assert.Throws<DomainException>(() =>
            Expense.Record(Id, User, Account, Category, null,
                0m, Currency.PLN, new DateOnly(2026, 3, 25), "Bad"));
    }

    [Fact]
    public void Record_rejects_empty_description()
    {
        Assert.Throws<DomainException>(() =>
            Expense.Record(Id, User, Account, Category, null,
                100m, Currency.PLN, new DateOnly(2026, 3, 25), ""));
    }

    [Fact]
    public void ChangeAmount_updates_value()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.ClearUncommittedEvents();

        expense.ChangeAmount(150m, Currency.PLN);

        Assert.Equal(150m, expense.Amount);
    }

    [Fact]
    public void Recategorize_changes_category()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.ClearUncommittedEvents();

        expense.Recategorize(new CategoryId("cat-2"), new SubcategoryId("sub-1"));

        Assert.Equal(new CategoryId("cat-2"), expense.CategoryId);
        Assert.Equal(new SubcategoryId("sub-1"), expense.SubcategoryId);
    }

    [Fact]
    public void Update_modifies_fields()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.ClearUncommittedEvents();

        expense.Update(amount: 150m, description: "Lunch");

        Assert.Equal(150m, expense.Amount);
        Assert.Equal("Lunch", expense.Description);
    }

    [Fact]
    public void Update_rejects_negative_amount()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");

        Assert.Throws<DomainException>(() => expense.Update(amount: -5m));
    }

    [Fact]
    public void Delete_marks_as_deleted()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.True(expense.IsDeleted);
    }

    [Fact]
    public void Cannot_update_deleted_expense()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.Throws<DomainException>(() => expense.Update(amount: 200m));
    }

    [Fact]
    public void Cannot_delete_already_deleted_expense()
    {
        var expense = Expense.Record(Id, User, Account, Category, null,
            100m, Currency.PLN, new DateOnly(2026, 3, 25), "Coffee");
        expense.Delete();

        Assert.Throws<DomainException>(() => expense.Delete());
    }
}

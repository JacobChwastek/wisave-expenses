using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Accounting;

public class AccountTests
{
    [Fact]
    public void Open_creates_account_with_correct_state()
    {
        var account = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);

        Assert.Equal("acc-1", account.Id);
        Assert.Equal("user-1", account.UserId);
        Assert.Equal("mBank", account.Name);
        Assert.True(account.IsActive);
        Assert.Single(account.GetUncommittedEvents());
        Assert.IsType<AccountOpened>(account.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Update_modifies_account_fields()
    {
        var account = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.ClearUncommittedEvents();

        account.Update("mBank Main", AccountType.BankAccount, Currency.PLN, 2000m, color: "#3b82f6");

        Assert.Equal("mBank Main", account.Name);
        Assert.Equal(2000m, account.Balance);
        Assert.Single(account.GetUncommittedEvents());
    }

    [Fact]
    public void Close_deactivates_account()
    {
        var account = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.False(account.IsActive);
    }

    [Fact]
    public void Cannot_modify_closed_account()
    {
        var account = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.Throws<DomainException>(() =>
            account.Update("New Name", AccountType.BankAccount, Currency.PLN, 1000m));
    }

    [Fact]
    public void Cannot_close_already_closed_account()
    {
        var account = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.Throws<DomainException>(() => account.Close());
    }

    [Fact]
    public void Replay_restores_state_from_events()
    {
        var original = Account.Open("acc-1", "user-1", "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        original.Update("mBank Main", AccountType.BankAccount, Currency.PLN, 2000m);
        var events = original.GetUncommittedEvents();

        var replayed = new Account();
        replayed.ReplayEvents(events);

        Assert.Equal("acc-1", replayed.Id);
        Assert.Equal("mBank Main", replayed.Name);
        Assert.Equal(2000m, replayed.Balance);
    }
}

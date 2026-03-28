using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Accounting;

public class AccountTests
{
    private static readonly AccountId Id = new("acc-1");
    private static readonly UserId User = new("user-1");

    [Fact]
    public void Open_creates_account_with_correct_state()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);

        Assert.Equal("acc-1", account.Id);
        Assert.Equal(User, account.UserId);
        Assert.Equal("mBank", account.Name);
        Assert.True(account.IsActive);
        Assert.Single(account.GetUncommittedEvents());
        Assert.IsType<AccountOpened>(account.GetUncommittedEvents()[0]);
    }

    [Fact]
    public void Update_modifies_account_fields()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.ClearUncommittedEvents();

        account.Update("mBank Main", AccountType.BankAccount, Currency.PLN, 2000m, color: "#3b82f6");

        Assert.Equal("mBank Main", account.Name);
        Assert.Equal(2000m, account.Balance);
        Assert.Single(account.GetUncommittedEvents());
    }

    [Fact]
    public void Rename_changes_name()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Rename("mBank Main");

        Assert.Equal("mBank Main", account.Name);
    }

    [Fact]
    public void LinkToBankAccount_sets_linked_id()
    {
        var account = Account.Open(Id, User, "Visa", AccountType.CreditCard, Currency.PLN, 0m, creditLimit: 5000m);
        var bankId = new AccountId("acc-bank");
        account.LinkToBankAccount(bankId);

        Assert.Equal(bankId, account.LinkedBankAccountId);
    }

    [Fact]
    public void Close_deactivates_account()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.False(account.IsActive);
    }

    [Fact]
    public void Cannot_modify_closed_account()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.Throws<DomainException>(() =>
            account.Update("New Name", AccountType.BankAccount, Currency.PLN, 1000m));
    }

    [Fact]
    public void Cannot_close_already_closed_account()
    {
        var account = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        account.Close();

        Assert.Throws<DomainException>(() => account.Close());
    }

    [Fact]
    public void Replay_restores_state_from_events()
    {
        var original = Account.Open(Id, User, "mBank", AccountType.BankAccount, Currency.PLN, 1000m);
        original.Update("mBank Main", AccountType.BankAccount, Currency.PLN, 2000m);
        var events = original.GetUncommittedEvents();

        var replayed = new Account();
        replayed.ReplayEvents(events);

        Assert.Equal("acc-1", replayed.Id);
        Assert.Equal("mBank Main", replayed.Name);
        Assert.Equal(2000m, replayed.Balance);
    }
}

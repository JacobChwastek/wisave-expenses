using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.Accounting;

public sealed class Account : AggregateRoot
{
    public UserId UserId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public AccountId? LinkedBankAccountId { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public BillingCycleDay? BillingCycleDay { get; private set; }
    public string? Color { get; private set; }
    public string? LastFourDigits { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Account() { }

    public static Account Open(
        AccountId id, UserId userId, string name, AccountType type, Currency currency, decimal balance,
        AccountId? linkedBankAccountId = null, decimal? creditLimit = null, int? billingCycleDay = null,
        string? color = null, string? lastFourDigits = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name is required.");
        if (billingCycleDay.HasValue) _ = new BillingCycleDay(billingCycleDay.Value);
        if (creditLimit is < 0)
            throw new DomainException("Credit limit cannot be negative.");

        var account = new Account();
        account.RaiseEvent(new AccountOpened(
            id.Value, userId.Value, name, type, currency, balance,
            linkedBankAccountId?.Value, creditLimit, billingCycleDay, color, lastFourDigits,
            DateTimeOffset.UtcNow));
        return account;
    }

    public void Rename(string name)
    {
        EnsureActive();
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name is required.");

        RaiseEvent(new AccountUpdated(
            Id, UserId.Value, name, Type, Currency, Balance,
            LinkedBankAccountId?.Value, CreditLimit, BillingCycleDay?.Value, Color, LastFourDigits,
            DateTimeOffset.UtcNow));
    }

    public void ChangeCreditLimit(decimal limit)
    {
        EnsureActive();
        if (Type != AccountType.CreditCard)
            throw new DomainException("Credit limit can only be set on credit cards.");
        if (limit < 0)
            throw new DomainException("Credit limit cannot be negative.");

        RaiseEvent(new AccountUpdated(
            Id, UserId.Value, Name, Type, Currency, Balance,
            LinkedBankAccountId?.Value, limit, BillingCycleDay?.Value, Color, LastFourDigits,
            DateTimeOffset.UtcNow));
    }

    public void LinkToBankAccount(AccountId bankAccountId)
    {
        EnsureActive();
        if (Type is not (AccountType.DebitCard or AccountType.CreditCard))
            throw new DomainException("Only cards can be linked to a bank account.");

        RaiseEvent(new AccountUpdated(
            Id, UserId.Value, Name, Type, Currency, Balance,
            bankAccountId.Value, CreditLimit, BillingCycleDay?.Value, Color, LastFourDigits,
            DateTimeOffset.UtcNow));
    }

    public void SetBillingCycleDay(BillingCycleDay day)
    {
        EnsureActive();
        if (Type != AccountType.CreditCard)
            throw new DomainException("Billing cycle day can only be set on credit cards.");

        RaiseEvent(new AccountUpdated(
            Id, UserId.Value, Name, Type, Currency, Balance,
            LinkedBankAccountId?.Value, CreditLimit, day.Value, Color, LastFourDigits,
            DateTimeOffset.UtcNow));
    }

    public void Update(
        string name, AccountType type, Currency currency, decimal balance,
        AccountId? linkedBankAccountId = null, decimal? creditLimit = null, int? billingCycleDay = null,
        string? color = null, string? lastFourDigits = null)
    {
        EnsureActive();
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Account name is required.");
        if (billingCycleDay.HasValue) _ = new BillingCycleDay(billingCycleDay.Value);

        RaiseEvent(new AccountUpdated(
            Id, UserId.Value, name, type, currency, balance,
            linkedBankAccountId?.Value, creditLimit, billingCycleDay, color, lastFourDigits,
            DateTimeOffset.UtcNow));
    }

    public void Close()
    {
        EnsureActive();
        RaiseEvent(new AccountClosed(Id, UserId.Value, DateTimeOffset.UtcNow));
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify a closed account.");
    }

    #region Apply

    public void Apply(AccountOpened e)
    {
        Id = e.AccountId;
        UserId = new UserId(e.UserId);
        Name = e.Name;
        Type = e.Type;
        Currency = e.Currency;
        Balance = e.Balance;
        LinkedBankAccountId = e.LinkedBankAccountId is not null ? new AccountId(e.LinkedBankAccountId) : null;
        CreditLimit = e.CreditLimit;
        BillingCycleDay = e.BillingCycleDay.HasValue ? new BillingCycleDay(e.BillingCycleDay.Value) : null;
        Color = e.Color;
        LastFourDigits = e.LastFourDigits;
        IsActive = true;
    }

    public void Apply(AccountUpdated e)
    {
        Name = e.Name;
        Type = e.Type;
        Currency = e.Currency;
        Balance = e.Balance;
        LinkedBankAccountId = e.LinkedBankAccountId is not null ? new AccountId(e.LinkedBankAccountId) : null;
        CreditLimit = e.CreditLimit;
        BillingCycleDay = e.BillingCycleDay.HasValue ? new BillingCycleDay(e.BillingCycleDay.Value) : null;
        Color = e.Color;
        LastFourDigits = e.LastFourDigits;
    }

    public void Apply(AccountClosed e)
    {
        IsActive = false;
    }

    #endregion
}

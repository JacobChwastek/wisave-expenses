using WiSave.Expenses.Core.Domain.Accounting.Events;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Accounting;

public sealed class Account : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public string? LinkedBankAccountId { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public int? BillingCycleDay { get; private set; }
    public string? Color { get; private set; }
    public string? LastFourDigits { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Account() { }

    public static Account Open(
        string id, string userId, string name, string type, string currency, decimal balance,
        string? linkedBankAccountId = null, decimal? creditLimit = null, int? billingCycleDay = null,
        string? color = null, string? lastFourDigits = null)
    {
        var account = new Account();
        account.RaiseEvent(new AccountOpenedEvent(
            id, userId, name, type, currency, balance,
            linkedBankAccountId, creditLimit, billingCycleDay, color, lastFourDigits));
        return account;
    }

    public void Update(
        string name, string type, string currency, decimal balance,
        string? linkedBankAccountId = null, decimal? creditLimit = null, int? billingCycleDay = null,
        string? color = null, string? lastFourDigits = null)
    {
        EnsureActive();
        RaiseEvent(new AccountUpdatedEvent(
            Id, name, type, currency, balance,
            linkedBankAccountId, creditLimit, billingCycleDay, color, lastFourDigits));
    }

    public void Close()
    {
        EnsureActive();
        RaiseEvent(new AccountClosedEvent(Id));
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify a closed account.");
    }

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case AccountOpenedEvent e:
                Id = e.AccountId;
                UserId = e.UserId;
                Name = e.Name;
                Type = e.Type;
                Currency = e.Currency;
                Balance = e.Balance;
                LinkedBankAccountId = e.LinkedBankAccountId;
                CreditLimit = e.CreditLimit;
                BillingCycleDay = e.BillingCycleDay;
                Color = e.Color;
                LastFourDigits = e.LastFourDigits;
                IsActive = true;
                break;

            case AccountUpdatedEvent e:
                Name = e.Name;
                Type = e.Type;
                Currency = e.Currency;
                Balance = e.Balance;
                LinkedBankAccountId = e.LinkedBankAccountId;
                CreditLimit = e.CreditLimit;
                BillingCycleDay = e.BillingCycleDay;
                Color = e.Color;
                LastFourDigits = e.LastFourDigits;
                break;

            case AccountClosedEvent:
                IsActive = false;
                break;
        }
    }
}

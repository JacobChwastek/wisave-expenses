using MassTransit;
using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class AccountEventHandler(ProjectionsDbContext db) :
    IConsumer<AccountOpened>,
    IConsumer<AccountUpdated>,
    IConsumer<AccountClosed>
{
    public async Task Consume(ConsumeContext<AccountOpened> context)
    {
        var e = context.Message;
        db.Accounts.Add(new AccountReadModel
        {
            Id = e.AccountId,
            UserId = e.UserId,
            Name = e.Name,
            Type = e.Type.ToString(),
            Currency = e.Currency.ToString(),
            Balance = e.Balance,
            CreditLimit = e.CreditLimit,
            BillingCycleDay = e.BillingCycleDay,
            LinkedBankAccountId = e.LinkedBankAccountId,
            Color = e.Color,
            LastFourDigits = e.LastFourDigits,
            IsActive = true,
            CreatedAt = e.Timestamp,
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<AccountUpdated> context)
    {
        var e = context.Message;
        var account = await db.Accounts.FindAsync([e.AccountId], context.CancellationToken);
        if (account is null) return;

        account.Name = e.Name;
        account.Type = e.Type.ToString();
        account.Currency = e.Currency.ToString();
        account.Balance = e.Balance;
        account.CreditLimit = e.CreditLimit;
        account.BillingCycleDay = e.BillingCycleDay;
        account.LinkedBankAccountId = e.LinkedBankAccountId;
        account.Color = e.Color;
        account.LastFourDigits = e.LastFourDigits;
        account.UpdatedAt = e.Timestamp;

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<AccountClosed> context)
    {
        var e = context.Message;
        var account = await db.Accounts.FindAsync([e.AccountId], context.CancellationToken);
        if (account is null) return;

        account.IsActive = false;
        account.UpdatedAt = e.Timestamp;

        await db.SaveChangesAsync(context.CancellationToken);
    }
}

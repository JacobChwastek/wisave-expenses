using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class AccountEventHandler(ProjectionsDbContext db) :
    IConsumer<AccountOpened>,
    IConsumer<AccountUpdated>,
    IConsumer<AccountClosed>
{
    public Task Consume(ConsumeContext<AccountOpened> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(AccountOpened message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, () =>
        {
            db.Accounts.Add(new AccountReadModel
            {
                Id = message.AccountId,
                UserId = message.UserId,
                Name = message.Name,
                Type = message.Type.ToString(),
                Currency = message.Currency.ToString(),
                Balance = message.Balance,
                CreditLimit = message.CreditLimit,
                BillingCycleDay = message.BillingCycleDay,
                LinkedBankAccountId = message.LinkedBankAccountId,
                Color = message.Color,
                LastFourDigits = message.LastFourDigits,
                IsActive = true,
                CreatedAt = message.Timestamp,
            });

            return Task.CompletedTask;
        });
    }

    public Task Consume(ConsumeContext<AccountUpdated> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(AccountUpdated message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var account = await db.Accounts.FindAsync([message.AccountId], ct);
            if (account is null) return;

            account.Name = message.Name;
            account.Type = message.Type.ToString();
            account.Currency = message.Currency.ToString();
            account.Balance = message.Balance;
            account.CreditLimit = message.CreditLimit;
            account.BillingCycleDay = message.BillingCycleDay;
            account.LinkedBankAccountId = message.LinkedBankAccountId;
            account.Color = message.Color;
            account.LastFourDigits = message.LastFourDigits;
            account.UpdatedAt = message.Timestamp;
        });
    }

    public Task Consume(ConsumeContext<AccountClosed> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(AccountClosed message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var account = await db.Accounts.FindAsync([message.AccountId], ct);
            if (account is null) return;

            account.IsActive = false;
            account.UpdatedAt = message.Timestamp;
        });
    }

    private async Task ExecuteIdempotentAsync(Guid? messageId, CancellationToken ct, Func<Task> applyChanges)
    {
        var requiredMessageId = messageId ?? throw new InvalidOperationException("MessageId header is required.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedMessages.AnyAsync(x => x.MessageId == requiredMessageId, ct))
            return;

        await applyChanges();

        db.ProcessedMessages.Add(new ProcessedMessageReadModel
        {
            MessageId = requiredMessageId,
            ProcessedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}

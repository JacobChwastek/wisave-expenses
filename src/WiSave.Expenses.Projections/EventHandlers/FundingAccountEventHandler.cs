using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class FundingAccountEventHandler(ProjectionsDbContext db) :
    IConsumer<FundingAccountOpened>,
    IConsumer<FundingAccountUpdated>,
    IConsumer<FundingAccountClosed>,
    IConsumer<FundingTransferPosted>,
    IConsumer<FundingPaymentInstrumentAdded>,
    IConsumer<FundingPaymentInstrumentUpdated>,
    IConsumer<FundingPaymentInstrumentRemoved>
{
    public Task Consume(ConsumeContext<FundingAccountOpened> context)
    {
        var message = context.Message;

        db.FundingAccounts.Add(new FundingAccountReadModel
        {
            Id = message.FundingAccountId,
            UserId = message.UserId,
            Name = message.Name,
            Kind = message.Kind.ToString(),
            Currency = message.Currency.ToString(),
            Balance = message.OpeningBalance,
            Color = message.Color,
            IsActive = true,
            CreatedAt = message.Timestamp,
        });

        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<FundingAccountUpdated> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.FundingAccounts.FindAsync([message.FundingAccountId], ct);
        if (account is null) return;

        account.Name = message.Name;
        account.Kind = message.Kind.ToString();
        account.Currency = message.Currency.ToString();
        account.Color = message.Color;
        account.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<FundingAccountClosed> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.FundingAccounts.FindAsync([message.FundingAccountId], ct);
        if (account is null) return;

        account.IsActive = false;
        account.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<FundingTransferPosted> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.FundingAccounts.FindAsync([message.FundingAccountId], ct);
        if (account is null) return;

        account.Balance -= message.Amount;
        account.UpdatedAt = message.Timestamp;
    }

    public Task Consume(ConsumeContext<FundingPaymentInstrumentAdded> context)
    {
        var message = context.Message;

        db.FundingPaymentInstruments.Add(new FundingPaymentInstrumentReadModel
        {
            Id = message.PaymentInstrumentId,
            FundingAccountId = message.FundingAccountId,
            UserId = message.UserId,
            Name = message.Name,
            Kind = message.Kind.ToString(),
            LastFourDigits = message.LastFourDigits,
            Network = message.Network,
            Color = message.Color,
            IsActive = true,
            CreatedAt = message.Timestamp,
        });

        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<FundingPaymentInstrumentUpdated> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var instrument = await db.FundingPaymentInstruments.FindAsync(
            [message.FundingAccountId, message.PaymentInstrumentId], ct);
        if (instrument is null) return;

        instrument.Name = message.Name;
        instrument.Kind = message.Kind.ToString();
        instrument.LastFourDigits = message.LastFourDigits;
        instrument.Network = message.Network;
        instrument.Color = message.Color;
        instrument.IsActive = true;
        instrument.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<FundingPaymentInstrumentRemoved> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var instrument = await db.FundingPaymentInstruments.FindAsync(
            [message.FundingAccountId, message.PaymentInstrumentId], ct);
        if (instrument is null) return;

        instrument.IsActive = false;
        instrument.UpdatedAt = message.Timestamp;
    }
}

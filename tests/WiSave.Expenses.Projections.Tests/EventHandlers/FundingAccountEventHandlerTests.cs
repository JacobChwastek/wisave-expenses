using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Projections.Tests.EventHandlers;

public class FundingAccountEventHandlerTests
{
    [Fact]
    public async Task FundingAccountOpened_creates_active_read_model()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var timestamp = DateTimeOffset.Parse("2026-04-23T10:00:00Z");

        await harness.PublishAsync(new FundingAccountOpened(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            Name: "Main checking",
            Kind: FundingAccountKind.BankAccount,
            Currency: Currency.PLN,
            OpeningBalance: 1500m,
            Color: "#3b82f6",
            Timestamp: timestamp));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.FundingAccounts.SingleAsync();
            Assert.Equal("fund-1", account.Id);
            Assert.Equal("user-1", account.UserId);
            Assert.Equal("Main checking", account.Name);
            Assert.Equal("BankAccount", account.Kind);
            Assert.Equal("PLN", account.Currency);
            Assert.Equal(1500m, account.Balance);
            Assert.Equal("#3b82f6", account.Color);
            Assert.True(account.IsActive);
            Assert.Equal(timestamp, account.CreatedAt);
            Assert.Null(account.UpdatedAt);
        });
    }

    [Fact]
    public async Task FundingAccountUpdated_updates_read_model()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var openedAt = DateTimeOffset.Parse("2026-04-23T10:00:00Z");
        var updatedAt = DateTimeOffset.Parse("2026-04-23T11:00:00Z");

        await harness.PublishAsync(new FundingAccountOpened(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6",
            openedAt));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.FundingAccounts.CountAsync()));

        await harness.PublishAsync(new FundingAccountUpdated(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            Name: "Emergency cash",
            Kind: FundingAccountKind.Cash,
            Currency: Currency.EUR,
            Color: "#10b981",
            Timestamp: updatedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.FundingAccounts.SingleAsync(x => x.Id == "fund-1");
            Assert.Equal("Emergency cash", account.Name);
            Assert.Equal("Cash", account.Kind);
            Assert.Equal("EUR", account.Currency);
            Assert.Equal(1500m, account.Balance);
            Assert.Equal("#10b981", account.Color);
            Assert.Equal(openedAt, account.CreatedAt);
            Assert.Equal(updatedAt, account.UpdatedAt);
        });
    }

    [Fact]
    public async Task FundingTransferPosted_reduces_balance()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var postedAt = DateTimeOffset.Parse("2026-05-20T10:00:00Z");

        await harness.PublishAsync(new FundingAccountOpened(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6",
            DateTimeOffset.Parse("2026-04-23T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.FundingAccounts.CountAsync()));

        await harness.PublishAsync(new FundingTransferPosted(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            TransferId: "transfer-1",
            TargetCreditCardAccountId: "card-1",
            StatementId: "stmt-1",
            Amount: 700m,
            PostedAtUtc: postedAt,
            Timestamp: postedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.FundingAccounts.SingleAsync(x => x.Id == "fund-1");
            Assert.Equal(800m, account.Balance);
            Assert.Equal(postedAt, account.UpdatedAt);
        });
    }

    [Fact]
    public async Task FundingPaymentInstrumentAdded_creates_child_read_model()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var timestamp = DateTimeOffset.Parse("2026-04-23T11:00:00Z");

        await harness.PublishAsync(new FundingAccountOpened(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6",
            DateTimeOffset.Parse("2026-04-23T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.FundingAccounts.CountAsync()));

        await harness.PublishAsync(new FundingPaymentInstrumentAdded(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            PaymentInstrumentId: "pi-1",
            Name: "mBank debit",
            Kind: PaymentInstrumentKind.DebitCard,
            LastFourDigits: "4532",
            Network: "Visa",
            Color: "#0f766e",
            Timestamp: timestamp));

        await harness.EventuallyAsync(async db =>
        {
            var instrument = await db.FundingPaymentInstruments.SingleAsync();
            Assert.Equal("pi-1", instrument.Id);
            Assert.Equal("fund-1", instrument.FundingAccountId);
            Assert.Equal("DebitCard", instrument.Kind);
            Assert.Equal("4532", instrument.LastFourDigits);
            Assert.True(instrument.IsActive);
            Assert.Equal(timestamp, instrument.CreatedAt);
        });
    }

    [Fact]
    public async Task FundingPaymentInstrumentRemoved_marks_child_read_model_inactive()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();

        await harness.PublishAsync(new FundingPaymentInstrumentAdded(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            PaymentInstrumentId: "pi-1",
            Name: "mBank debit",
            Kind: PaymentInstrumentKind.DebitCard,
            LastFourDigits: "4532",
            Network: "Visa",
            Color: "#0f766e",
            Timestamp: DateTimeOffset.Parse("2026-04-23T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.FundingPaymentInstruments.CountAsync()));

        var removedAt = DateTimeOffset.Parse("2026-04-23T12:00:00Z");
        await harness.PublishAsync(new FundingPaymentInstrumentRemoved(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            PaymentInstrumentId: "pi-1",
            Timestamp: removedAt));

        await harness.EventuallyAsync(async db =>
        {
            var instrument = await db.FundingPaymentInstruments.SingleAsync();
            Assert.False(instrument.IsActive);
            Assert.Equal(removedAt, instrument.UpdatedAt);
        });
    }

    [Fact]
    public async Task FundingAccountClosed_marks_read_model_inactive()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var closedAt = DateTimeOffset.Parse("2026-04-23T12:00:00Z");

        await harness.PublishAsync(new FundingAccountOpened(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6",
            DateTimeOffset.Parse("2026-04-23T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.FundingAccounts.CountAsync()));

        await harness.PublishAsync(new FundingAccountClosed(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            Timestamp: closedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.FundingAccounts.SingleAsync(x => x.Id == "fund-1");
            Assert.False(account.IsActive);
            Assert.Equal(closedAt, account.UpdatedAt);
        });
    }
}

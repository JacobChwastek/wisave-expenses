using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Projections.EventHandlers;

namespace WiSave.Expenses.Projections.Tests.EventHandlers;

public class ProjectionIdempotencyFilterTests
{
    [Fact]
    public async Task Filter_records_message_and_skips_duplicate_delivery()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var consumerHarness = harness.Harness.GetConsumerHarness<FundingAccountEventHandler>();

        var messageId = Guid.NewGuid();
        var message = new FundingAccountOpened(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            Name: "Main checking",
            Currency: Currency.PLN,
            Kind: FundingAccountKind.BankAccount,
            OpeningBalance: 1500m,
            Color: "#3b82f6",
            Timestamp: DateTimeOffset.UtcNow);

        await harness.Harness.Bus.Publish(message, context => context.MessageId = messageId);
        Assert.True(await consumerHarness.Consumed.Any<FundingAccountOpened>());

        await harness.Harness.Bus.Publish(message, context => context.MessageId = messageId);
        await Task.Delay(250);

        Assert.Single(consumerHarness.Consumed.Select<FundingAccountOpened>());
        await harness.EventuallyAsync(async db =>
        {
            Assert.Equal(1, await db.FundingAccounts.CountAsync());
            Assert.Equal(1, await db.ProcessedMessages.CountAsync());
        });

        Assert.False(await harness.Harness.Published.Any<Fault<FundingAccountOpened>>());
    }
}

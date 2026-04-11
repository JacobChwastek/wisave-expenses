using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Projections.EventHandlers;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.Tests.EventHandlers;

public class ProjectionIdempotencyTests
{
    [Fact]
    public async Task Duplicate_message_id_is_ignored_on_second_delivery()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new AccountEventHandler(db);
        var messageId = Guid.NewGuid();
        var message = new AccountOpened(
            AccountId: "acc-1",
            UserId: "user-1",
            Name: "Primary",
            Type: AccountType.BankAccount,
            Currency: Currency.USD,
            Balance: 250m,
            LinkedBankAccountId: null,
            CreditLimit: null,
            BillingCycleDay: null,
            Color: null,
            LastFourDigits: "1234",
            Timestamp: DateTimeOffset.UtcNow);

        await handler.HandleAsync(message, messageId, CancellationToken.None);
        await handler.HandleAsync(message, messageId, CancellationToken.None);

        Assert.Equal(1, await db.Accounts.CountAsync());
        Assert.Equal(1, await db.Set<ProcessedMessageReadModel>().CountAsync());
    }
}

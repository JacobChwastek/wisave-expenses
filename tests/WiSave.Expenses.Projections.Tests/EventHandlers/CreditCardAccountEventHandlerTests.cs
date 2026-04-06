using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.CreditCards;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Projections.Tests.EventHandlers;

public class CreditCardAccountEventHandlerTests
{
    [Fact]
    public async Task CreditCardAccountOpened_creates_active_read_model()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var timestamp = DateTimeOffset.Parse("2026-04-23T10:00:00Z");

        await harness.PublishAsync(new CreditCardAccountOpened(
            CreditCardAccountId: "card-1",
            UserId: "user-1",
            Name: "mBank Visa",
            Currency: Currency.PLN,
            SettlementAccountId: "fund-1",
            BankProvider: BankProvider.MBank,
            ProductCode: "STANDARD",
            CreditLimit: 12000m,
            StatementClosingDay: 16,
            GracePeriodDays: 24,
            Color: "#f59e0b",
            LastFourDigits: "4532",
            Timestamp: timestamp));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.CreditCardAccounts.SingleAsync();
            Assert.Equal("card-1", account.Id);
            Assert.Equal("user-1", account.UserId);
            Assert.Equal("mBank Visa", account.Name);
            Assert.Equal("PLN", account.Currency);
            Assert.Equal("fund-1", account.SettlementAccountId);
            Assert.Equal("MBank", account.BankProvider);
            Assert.Equal("STANDARD", account.ProductCode);
            Assert.Equal(12000m, account.CreditLimit);
            Assert.Equal(0m, account.UnbilledBalance);
            Assert.Equal(16, account.StatementClosingDay);
            Assert.Equal(24, account.GracePeriodDays);
            Assert.Equal("#f59e0b", account.Color);
            Assert.Equal("4532", account.LastFourDigits);
            Assert.True(account.IsActive);
            Assert.Equal(timestamp, account.CreatedAt);
        });
    }

    [Fact]
    public async Task CreditCardStatementIssued_creates_summary_and_history_rows()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var issuedAt = DateTimeOffset.Parse("2026-05-16T10:00:00Z");

        await OpenCardAsync(harness);

        await harness.PublishAsync(new CreditCardStatementIssued(
            CreditCardAccountId: "card-1",
            StatementId: "stmt-1",
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04",
            Timestamp: issuedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.CreditCardAccounts.SingleAsync(x => x.Id == "card-1");
            Assert.Equal(7000m, account.ActiveStatementBalance);
            Assert.Equal(7000m, account.ActiveStatementOutstandingBalance);
            Assert.Equal(350m, account.ActiveStatementMinimumPaymentDue);
            Assert.Equal(new DateOnly(2026, 6, 9), account.ActiveStatementDueDate);
            Assert.Equal(new DateOnly(2026, 5, 16), account.ActiveStatementPeriodCloseDate);
            Assert.Equal(3458m, account.UnbilledBalance);
            Assert.Equal(issuedAt, account.UpdatedAt);

            var statement = await db.CreditCardStatements.SingleAsync();
            Assert.Equal("stmt-1", statement.Id);
            Assert.Equal("card-1", statement.CreditCardAccountId);
            Assert.Equal(new DateOnly(2026, 4, 17), statement.PeriodFrom);
            Assert.Equal(new DateOnly(2026, 5, 16), statement.PeriodTo);
            Assert.Equal(new DateOnly(2026, 5, 16), statement.StatementDate);
            Assert.Equal(new DateOnly(2026, 6, 9), statement.DueDate);
            Assert.Equal(7000m, statement.StatementBalance);
            Assert.Equal(7000m, statement.OutstandingBalance);
            Assert.Equal(350m, statement.MinimumPaymentDue);
            Assert.Equal("MBANK_STANDARD", statement.PolicyCode);
            Assert.Equal("2026-04", statement.PolicyVersion);
            Assert.Equal(issuedAt, statement.CreatedAt);
        });
    }

    [Fact]
    public async Task CreditCardStatementIssued_allows_same_statement_id_on_different_cards()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();

        await OpenCardAsync(harness, "card-1", "user-1", "mBank Visa");
        await OpenCardAsync(harness, "card-2", "user-2", "mBank Mastercard");

        await IssueStatementAsync(harness, "card-1", "stmt-1", 7000m);
        await IssueStatementAsync(harness, "card-2", "stmt-1", 4200m);

        await harness.EventuallyAsync(async db =>
        {
            Assert.Equal(2, await db.CreditCardStatements.CountAsync());
            Assert.Equal(7000m, await db.CreditCardStatements
                .Where(x => x.CreditCardAccountId == "card-1" && x.Id == "stmt-1")
                .Select(x => x.StatementBalance)
                .SingleAsync());
            Assert.Equal(4200m, await db.CreditCardStatements
                .Where(x => x.CreditCardAccountId == "card-2" && x.Id == "stmt-1")
                .Select(x => x.StatementBalance)
                .SingleAsync());
        });
    }

    [Fact]
    public async Task CreditCardStateSeeded_creates_seeded_statement_row_for_later_payment_projection()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var seededAt = DateTimeOffset.Parse("2026-05-16T10:00:00Z");
        var appliedAt = DateTimeOffset.Parse("2026-05-20T10:00:00Z");

        await OpenCardAsync(harness);
        await harness.PublishAsync(new CreditCardStateSeeded(
            CreditCardAccountId: "card-1",
            ActiveStatementId: "stmt-seeded",
            ActiveStatementBalance: 7000m,
            ActiveStatementMinimumPaymentDue: 350m,
            ActiveStatementPeriodCloseDate: new DateOnly(2026, 5, 16),
            ActiveStatementDueDate: new DateOnly(2026, 6, 9),
            UnbilledBalance: 3458m,
            Timestamp: seededAt));

        await harness.EventuallyAsync(async db =>
        {
            var seededStatement = await db.CreditCardStatements.SingleAsync();
            Assert.Equal("stmt-seeded", seededStatement.Id);
            Assert.Equal("card-1", seededStatement.CreditCardAccountId);
            Assert.Equal(7000m, seededStatement.OutstandingBalance);
            Assert.Equal("SEEDED", seededStatement.PolicyCode);
        });

        await harness.PublishAsync(new CreditCardStatementPaymentApplied(
            CreditCardAccountId: "card-1",
            StatementId: "stmt-seeded",
            TransferId: "transfer-1",
            Amount: 2500m,
            StatementOutstandingBalanceAfterApplication: 4500m,
            AppliedAtUtc: appliedAt,
            Timestamp: appliedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.CreditCardAccounts.SingleAsync(x => x.Id == "card-1");
            var seededStatement = await db.CreditCardStatements.SingleAsync();
            Assert.Equal(4500m, account.ActiveStatementOutstandingBalance);
            Assert.Equal(4500m, seededStatement.OutstandingBalance);
        });
    }

    [Fact]
    public async Task CreditCardStatementPaymentApplied_reduces_statement_and_active_outstanding()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();
        var appliedAt = DateTimeOffset.Parse("2026-05-20T10:00:00Z");

        await OpenCardAsync(harness);
        await harness.PublishAsync(new CreditCardStatementIssued(
            "card-1",
            "stmt-1",
            new DateOnly(2026, 4, 17),
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            7000m,
            350m,
            3458m,
            "MBANK_STANDARD",
            "2026-04",
            DateTimeOffset.Parse("2026-05-16T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.CreditCardStatements.CountAsync()));

        await harness.PublishAsync(new CreditCardStatementPaymentApplied(
            CreditCardAccountId: "card-1",
            StatementId: "stmt-1",
            TransferId: "transfer-1",
            Amount: 2500m,
            StatementOutstandingBalanceAfterApplication: 4500m,
            AppliedAtUtc: appliedAt,
            Timestamp: appliedAt));

        await harness.EventuallyAsync(async db =>
        {
            var account = await db.CreditCardAccounts.SingleAsync(x => x.Id == "card-1");
            Assert.Equal(4500m, account.ActiveStatementOutstandingBalance);
            Assert.Equal(appliedAt, account.UpdatedAt);

            var statement = await db.CreditCardStatements.SingleAsync(x => x.Id == "stmt-1");
            Assert.Equal(4500m, statement.OutstandingBalance);
            Assert.Equal(appliedAt, statement.UpdatedAt);
        });
    }

    private static async Task OpenCardAsync(
        ProjectionTestHarness harness,
        string cardId = "card-1",
        string userId = "user-1",
        string name = "mBank Visa")
    {
        await harness.PublishAsync(new CreditCardAccountOpened(
            cardId,
            userId,
            name,
            Currency.PLN,
            "fund-1",
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532",
            DateTimeOffset.Parse("2026-04-23T10:00:00Z")));
        await harness.EventuallyAsync(async db => Assert.True(await db.CreditCardAccounts.AnyAsync(x => x.Id == cardId)));
    }

    private static async Task IssueStatementAsync(
        ProjectionTestHarness harness,
        string cardId,
        string statementId,
        decimal statementBalance)
    {
        await harness.PublishAsync(new CreditCardStatementIssued(
            cardId,
            statementId,
            new DateOnly(2026, 4, 17),
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            statementBalance,
            Math.Round(statementBalance * 0.05m, 2, MidpointRounding.AwayFromZero),
            0m,
            "MBANK_STANDARD",
            "2026-04",
            DateTimeOffset.Parse("2026-05-16T10:00:00Z")));
    }
}

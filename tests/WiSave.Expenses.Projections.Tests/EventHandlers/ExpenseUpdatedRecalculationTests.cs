using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.Expenses;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Projections.Tests.EventHandlers;

public class ExpenseUpdatedRecalculationTests
{
    [Fact]
    public async Task ExpenseUpdated_recategorize_moves_spend_between_categories_without_amount_change()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();

        await harness.PublishAsync(
            new ExpenseRecorded(
                ExpenseId: "exp-1",
                UserId: "user-1",
                AccountId: "acc-1",
                CategoryId: "groceries",
                SubcategoryId: null,
                Amount: 100m,
                Currency: Currency.USD,
                Date: new DateOnly(2026, 3, 10),
                Description: "Weekly shop",
                Recurring: false,
                Metadata: null,
                Timestamp: DateTimeOffset.UtcNow));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.Expenses.CountAsync()));

        await harness.PublishAsync(
            new ExpenseUpdated(
                ExpenseId: "exp-1",
                UserId: "user-1",
                Amount: null,
                Currency: null,
                Date: null,
                Description: null,
                CategoryId: "dining",
                SubcategoryId: null,
                Recurring: null,
                Metadata: null,
                Timestamp: DateTimeOffset.UtcNow));

        await harness.EventuallyAsync(async db =>
        {
            var groceries = await db.SpendingSummaries.SingleAsync(x => x.CategoryId == "groceries");
            var dining = await db.SpendingSummaries.SingleAsync(x => x.CategoryId == "dining");

            Assert.Equal(0m, groceries.TotalSpent);
            Assert.Equal(100m, dining.TotalSpent);
        });
    }

    [Fact]
    public async Task ExpenseUpdated_date_change_moves_spend_between_months_without_amount_change()
    {
        await using var harness = await ProjectionTestHarness.StartAsync();

        await harness.PublishAsync(
            new ExpenseRecorded(
                ExpenseId: "exp-2",
                UserId: "user-1",
                AccountId: "acc-1",
                CategoryId: "groceries",
                SubcategoryId: null,
                Amount: 80m,
                Currency: Currency.USD,
                Date: new DateOnly(2026, 3, 10),
                Description: "Coffee beans",
                Recurring: false,
                Metadata: null,
                Timestamp: DateTimeOffset.UtcNow));
        await harness.EventuallyAsync(async db => Assert.Equal(1, await db.Expenses.CountAsync()));

        await harness.PublishAsync(
            new ExpenseUpdated(
                ExpenseId: "exp-2",
                UserId: "user-1",
                Amount: null,
                Currency: null,
                Date: new DateOnly(2026, 4, 5),
                Description: null,
                CategoryId: null,
                SubcategoryId: null,
                Recurring: null,
                Metadata: null,
                Timestamp: DateTimeOffset.UtcNow));

        await harness.EventuallyAsync(async db =>
        {
            var march = await db.MonthlyStats.SingleAsync(x => x.Month == 3 && x.Year == 2026);
            var april = await db.MonthlyStats.SingleAsync(x => x.Month == 4 && x.Year == 2026);

            Assert.Equal(0m, march.TotalSpent);
            Assert.Equal(80m, april.TotalSpent);
        });
    }
}

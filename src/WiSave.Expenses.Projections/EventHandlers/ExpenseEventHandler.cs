using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.Expenses;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class ExpenseEventHandler(ProjectionsDbContext db) :
    IConsumer<ExpenseRecorded>,
    IConsumer<ExpenseUpdated>,
    IConsumer<ExpenseDeleted>
{
    public async Task Consume(ConsumeContext<ExpenseRecorded> context)
    {
        var e = context.Message;
        db.Expenses.Add(new ExpenseReadModel
        {
            Id = e.ExpenseId,
            UserId = e.UserId,
            AccountId = e.AccountId,
            CategoryId = e.CategoryId,
            SubcategoryId = e.SubcategoryId,
            Amount = e.Amount,
            Currency = e.Currency.ToString(),
            Date = e.Date,
            Description = e.Description,
            Recurring = e.Recurring,
            MetadataJson = e.Metadata is not null ? JsonSerializer.Serialize(e.Metadata) : null,
            IsDeleted = false,
            CreatedAt = e.Timestamp,
        });

        await db.SaveChangesAsync(context.CancellationToken);
        await UpdateSpendingSummaryAsync(e.UserId, e.CategoryId, e.Date.Month, e.Date.Year, e.Amount, context.CancellationToken);
        await UpdateMonthlyStatsAsync(e.UserId, e.Date.Month, e.Date.Year, e.Amount, e.Currency.ToString(), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<ExpenseUpdated> context)
    {
        var e = context.Message;
        var expense = await db.Expenses.FindAsync([e.ExpenseId], context.CancellationToken);
        if (expense is null) return;

        var oldAmount = expense.Amount;
        var oldMonth = expense.Date.Month;
        var oldYear = expense.Date.Year;

        if (e.Amount.HasValue) expense.Amount = e.Amount.Value;
        if (e.Currency.HasValue) expense.Currency = e.Currency.Value.ToString();
        if (e.Date.HasValue) expense.Date = e.Date.Value;
        if (e.Description is not null) expense.Description = e.Description;
        if (e.CategoryId is not null) expense.CategoryId = e.CategoryId;
        if (e.SubcategoryId is not null) expense.SubcategoryId = e.SubcategoryId;
        if (e.Recurring.HasValue) expense.Recurring = e.Recurring.Value;
        if (e.Metadata is not null) expense.MetadataJson = JsonSerializer.Serialize(e.Metadata);
        expense.UpdatedAt = e.Timestamp;

        await db.SaveChangesAsync(context.CancellationToken);

        // Recalculate summaries if amount changed
        if (e.Amount.HasValue)
        {
            var delta = expense.Amount - oldAmount;
            await UpdateSpendingSummaryAsync(expense.UserId, expense.CategoryId, expense.Date.Month, expense.Date.Year, delta, context.CancellationToken);
            await UpdateMonthlyStatsAsync(expense.UserId, expense.Date.Month, expense.Date.Year, delta, expense.Currency, context.CancellationToken);
        }
    }

    public async Task Consume(ConsumeContext<ExpenseDeleted> context)
    {
        var e = context.Message;
        var expense = await db.Expenses.FindAsync([e.ExpenseId], context.CancellationToken);
        if (expense is null) return;

        expense.IsDeleted = true;
        expense.UpdatedAt = e.Timestamp;

        await db.SaveChangesAsync(context.CancellationToken);
        await UpdateSpendingSummaryAsync(expense.UserId, expense.CategoryId, expense.Date.Month, expense.Date.Year, -expense.Amount, context.CancellationToken);
        await UpdateMonthlyStatsAsync(expense.UserId, expense.Date.Month, expense.Date.Year, -expense.Amount, expense.Currency, context.CancellationToken);
    }

    private async Task UpdateSpendingSummaryAsync(string userId, string categoryId, int month, int year, decimal delta, CancellationToken ct)
    {
        var summary = await db.SpendingSummaries.FirstOrDefaultAsync(
            s => s.UserId == userId && s.CategoryId == categoryId && s.Month == month && s.Year == year, ct);

        if (summary is not null)
        {
            summary.TotalSpent += delta;
        }
        else
        {
            db.SpendingSummaries.Add(new SpendingSummaryReadModel
            {
                UserId = userId,
                CategoryId = categoryId,
                CategoryName = string.Empty, // Resolved by query service from config
                Month = month,
                Year = year,
                TotalSpent = delta,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task UpdateMonthlyStatsAsync(string userId, int month, int year, decimal delta, string currency, CancellationToken ct)
    {
        var stats = await db.MonthlyStats.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Month == month && s.Year == year, ct);

        if (stats is not null)
        {
            stats.TotalSpent += delta;
        }
        else
        {
            db.MonthlyStats.Add(new MonthlyStatsReadModel
            {
                UserId = userId,
                Month = month,
                Year = year,
                TotalSpent = delta,
                Currency = currency,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}

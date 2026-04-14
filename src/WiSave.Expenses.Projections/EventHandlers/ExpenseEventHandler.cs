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
    public Task Consume(ConsumeContext<ExpenseRecorded> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(ExpenseRecorded message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            db.Expenses.Add(new ExpenseReadModel
            {
                Id = message.ExpenseId,
                UserId = message.UserId,
                AccountId = message.AccountId,
                CategoryId = message.CategoryId,
                SubcategoryId = message.SubcategoryId,
                Amount = message.Amount,
                Currency = message.Currency.ToString(),
                Date = message.Date,
                Description = message.Description,
                Recurring = message.Recurring,
                MetadataJson = message.Metadata is not null ? JsonSerializer.Serialize(message.Metadata) : null,
                IsDeleted = false,
                CreatedAt = message.Timestamp,
            });

            await UpdateSpendingSummaryAsync(message.UserId, message.CategoryId, message.Date.Month, message.Date.Year, message.Amount, ct);
            await UpdateMonthlyStatsAsync(message.UserId, message.Date.Month, message.Date.Year, message.Amount, message.Currency.ToString(), ct);
        });
    }

    public Task Consume(ConsumeContext<ExpenseUpdated> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(ExpenseUpdated message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var expense = await db.Expenses.FindAsync([message.ExpenseId], ct);
            if (expense is null) return;

            var oldCategory = expense.CategoryId;
            var oldMonth = expense.Date.Month;
            var oldYear = expense.Date.Year;
            var oldAmount = expense.Amount;
            var oldCurrency = expense.Currency;

            if (message.Amount.HasValue) expense.Amount = message.Amount.Value;
            if (message.Currency.HasValue) expense.Currency = message.Currency.Value.ToString();
            if (message.Date.HasValue) expense.Date = message.Date.Value;
            if (message.Description is not null) expense.Description = message.Description;
            if (message.CategoryId is not null) expense.CategoryId = message.CategoryId;
            if (message.SubcategoryId is not null) expense.SubcategoryId = message.SubcategoryId;
            if (message.Recurring.HasValue) expense.Recurring = message.Recurring.Value;
            if (message.Metadata is not null) expense.MetadataJson = JsonSerializer.Serialize(message.Metadata);
            expense.UpdatedAt = message.Timestamp;

            var newCategory = expense.CategoryId;
            var newMonth = expense.Date.Month;
            var newYear = expense.Date.Year;
            var newAmount = expense.Amount;

            var movedCategory = oldCategory != newCategory;
            var movedPeriod = oldMonth != newMonth || oldYear != newYear;
            var changedAmount = oldAmount != newAmount;

            if (movedCategory || movedPeriod || changedAmount)
            {
                await UpdateSpendingSummaryAsync(expense.UserId, oldCategory, oldMonth, oldYear, -oldAmount, ct);
                await UpdateSpendingSummaryAsync(expense.UserId, newCategory, newMonth, newYear, newAmount, ct);
                await UpdateMonthlyStatsAsync(expense.UserId, oldMonth, oldYear, -oldAmount, oldCurrency, ct);
                await UpdateMonthlyStatsAsync(expense.UserId, newMonth, newYear, newAmount, expense.Currency, ct);
            }
        });
    }

    public Task Consume(ConsumeContext<ExpenseDeleted> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(ExpenseDeleted message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var expense = await db.Expenses.FindAsync([message.ExpenseId], ct);
            if (expense is null) return;

            expense.IsDeleted = true;
            expense.UpdatedAt = message.Timestamp;

            await UpdateSpendingSummaryAsync(expense.UserId, expense.CategoryId, expense.Date.Month, expense.Date.Year, -expense.Amount, ct);
            await UpdateMonthlyStatsAsync(expense.UserId, expense.Date.Month, expense.Date.Year, -expense.Amount, expense.Currency, ct);
        });
    }

    private async Task UpdateSpendingSummaryAsync(string userId, string categoryId, int month, int year, decimal delta, CancellationToken ct)
    {
        if (delta == 0)
        {
            return;
        }

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
    }

    private async Task UpdateMonthlyStatsAsync(string userId, int month, int year, decimal delta, string currency, CancellationToken ct)
    {
        if (delta == 0)
        {
            return;
        }

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

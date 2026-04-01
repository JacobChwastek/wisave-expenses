using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.Budgets;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class BudgetEventHandler(ProjectionsDbContext db) :
    IConsumer<BudgetCreated>,
    IConsumer<BudgetCopiedFromPrevious>,
    IConsumer<OverallLimitSet>,
    IConsumer<CategoryLimitSet>,
    IConsumer<CategoryLimitRemoved>
{
    public async Task Consume(ConsumeContext<BudgetCreated> context)
    {
        var e = context.Message;
        db.Budgets.Add(new BudgetReadModel
        {
            Id = e.BudgetId,
            UserId = e.UserId,
            Month = e.Month,
            Year = e.Year,
            TotalLimit = e.TotalLimit,
            Currency = e.Currency.ToString(),
            Recurring = e.Recurring,
            CreatedAt = e.Timestamp,
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<BudgetCopiedFromPrevious> context)
    {
        var e = context.Message;
        db.Budgets.Add(new BudgetReadModel
        {
            Id = e.BudgetId,
            UserId = e.UserId,
            Month = e.Month,
            Year = e.Year,
            TotalLimit = e.TotalLimit,
            Currency = e.Currency.ToString(),
            Recurring = e.Recurring,
            CreatedAt = e.Timestamp,
        });

        foreach (var (categoryId, limit) in e.CategoryLimits)
        {
            db.BudgetCategoryLimits.Add(new BudgetCategoryLimitReadModel
            {
                BudgetId = e.BudgetId,
                CategoryId = categoryId,
                Limit = limit,
            });
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<OverallLimitSet> context)
    {
        var e = context.Message;
        var budget = await db.Budgets.FindAsync([e.BudgetId], context.CancellationToken);
        if (budget is null) return;

        budget.TotalLimit = e.TotalLimit;
        budget.UpdatedAt = e.Timestamp;

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<CategoryLimitSet> context)
    {
        var e = context.Message;
        var existing = await db.BudgetCategoryLimits.FirstOrDefaultAsync(
            x => x.BudgetId == e.BudgetId && x.CategoryId == e.CategoryId, context.CancellationToken);

        if (existing is not null)
        {
            existing.Limit = e.Limit;
        }
        else
        {
            db.BudgetCategoryLimits.Add(new BudgetCategoryLimitReadModel
            {
                BudgetId = e.BudgetId,
                CategoryId = e.CategoryId,
                Limit = e.Limit,
            });
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<CategoryLimitRemoved> context)
    {
        var e = context.Message;
        var existing = await db.BudgetCategoryLimits.FirstOrDefaultAsync(
            x => x.BudgetId == e.BudgetId && x.CategoryId == e.CategoryId, context.CancellationToken);

        if (existing is not null)
        {
            db.BudgetCategoryLimits.Remove(existing);
            await db.SaveChangesAsync(context.CancellationToken);
        }
    }
}

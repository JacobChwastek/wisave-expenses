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
    public Task Consume(ConsumeContext<BudgetCreated> context)
    {
        var message = context.Message;

        db.Budgets.Add(new BudgetReadModel
        {
            Id = message.BudgetId,
            UserId = message.UserId,
            Month = message.Month,
            Year = message.Year,
            TotalLimit = message.TotalLimit,
            Currency = message.Currency.ToString(),
            Recurring = message.Recurring,
            CreatedAt = message.Timestamp,
        });

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<BudgetCopiedFromPrevious> context)
    {
        var message = context.Message;

        db.Budgets.Add(new BudgetReadModel
        {
            Id = message.BudgetId,
            UserId = message.UserId,
            Month = message.Month,
            Year = message.Year,
            TotalLimit = message.TotalLimit,
            Currency = message.Currency.ToString(),
            Recurring = message.Recurring,
            CreatedAt = message.Timestamp,
        });

        foreach (var (categoryId, limit) in message.CategoryLimits)
        {
            db.BudgetCategoryLimits.Add(new BudgetCategoryLimitReadModel
            {
                BudgetId = message.BudgetId,
                CategoryId = categoryId,
                Limit = limit,
            });
        }

        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<OverallLimitSet> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var budget = await db.Budgets.FindAsync([message.BudgetId], ct);
        if (budget is null) return;

        budget.TotalLimit = message.TotalLimit;
        budget.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<CategoryLimitSet> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var existing = await db.BudgetCategoryLimits.FirstOrDefaultAsync(
            x => x.BudgetId == message.BudgetId && x.CategoryId == message.CategoryId, ct);

        if (existing is not null)
        {
            existing.Limit = message.Limit;
        }
        else
        {
            db.BudgetCategoryLimits.Add(new BudgetCategoryLimitReadModel
            {
                BudgetId = message.BudgetId,
                CategoryId = message.CategoryId,
                Limit = message.Limit,
            });
        }
    }

    public async Task Consume(ConsumeContext<CategoryLimitRemoved> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var existing = await db.BudgetCategoryLimits.FirstOrDefaultAsync(
            x => x.BudgetId == message.BudgetId && x.CategoryId == message.CategoryId, ct);

        if (existing is not null)
        {
            db.BudgetCategoryLimits.Remove(existing);
        }
    }
}

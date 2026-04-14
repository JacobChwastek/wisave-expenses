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
    public Task Consume(ConsumeContext<BudgetCreated> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(BudgetCreated message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, () =>
        {
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
        });
    }

    public Task Consume(ConsumeContext<BudgetCopiedFromPrevious> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(BudgetCopiedFromPrevious message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, () =>
        {
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
        });
    }

    public Task Consume(ConsumeContext<OverallLimitSet> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(OverallLimitSet message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var budget = await db.Budgets.FindAsync([message.BudgetId], ct);
            if (budget is null) return;

            budget.TotalLimit = message.TotalLimit;
            budget.UpdatedAt = message.Timestamp;
        });
    }

    public Task Consume(ConsumeContext<CategoryLimitSet> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(CategoryLimitSet message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
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
        });
    }

    public Task Consume(ConsumeContext<CategoryLimitRemoved> context) =>
        HandleAsync(context.Message, context.MessageId, context.CancellationToken);

    public async Task HandleAsync(CategoryLimitRemoved message, Guid? messageId, CancellationToken ct)
    {
        await ExecuteIdempotentAsync(messageId, ct, async () =>
        {
            var existing = await db.BudgetCategoryLimits.FirstOrDefaultAsync(
                x => x.BudgetId == message.BudgetId && x.CategoryId == message.CategoryId, ct);

            if (existing is not null)
            {
                db.BudgetCategoryLimits.Remove(existing);
            }
        });
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

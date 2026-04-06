using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class IdempotentProjectionFilter<T>(ProjectionsDbContext db) : IFilter<ConsumeContext<T>>
    where T : class
{
    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("idempotent-projection");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageId = context.MessageId ?? throw new InvalidOperationException("MessageId header is required.");

        await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);
        if (await db.ProcessedMessages.AnyAsync(x => x.MessageId == messageId, context.CancellationToken))
            return;

        await next.Send(context);

        db.ProcessedMessages.Add(new ProcessedMessageReadModel
        {
            MessageId = messageId,
            ProcessedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(context.CancellationToken);
        await tx.CommitAsync(context.CancellationToken);
    }
}

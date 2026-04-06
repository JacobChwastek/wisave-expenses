using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class CloseCreditCardAccountHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository) : IConsumer<CloseCreditCardAccount>
{
    public async Task Consume(ConsumeContext<CloseCreditCardAccount> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;

        try
        {
            var account = await repository.LoadAsync(new CreditCardAccountId(command.CreditCardAccountId), ct);
            var guard = CommandGuard.Ok
                .Require(() => account is not null, "Credit card account not found.")
                .Require(() => account!.UserId == new UserId(command.UserId), "Access denied.");

            if (guard.HasFailed(out var reason))
            {
                await PublishFailed(context, command.CorrelationId, command.UserId, reason, ct);
                return;
            }

            account!.Close(DateTimeOffset.UtcNow);
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailed(context, command.CorrelationId, command.UserId, ex.Message, ct);
        }
    }

    private static Task PublishFailed(
        ConsumeContext<CloseCreditCardAccount> context,
        Guid correlationId,
        string userId,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            correlationId,
            userId,
            nameof(CloseCreditCardAccount),
            reason,
            DateTimeOffset.UtcNow), ct);
}

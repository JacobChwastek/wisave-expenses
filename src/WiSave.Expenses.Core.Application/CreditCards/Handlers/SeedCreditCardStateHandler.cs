using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class SeedCreditCardStateHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository) : IConsumer<SeedCreditCardState>
{
    public async Task Consume(ConsumeContext<SeedCreditCardState> context)
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

            var activeStatementId = command.ActiveStatementBalance > 0m
                ? new CreditCardStatementId($"stmt-{Guid.NewGuid():N}")
                : null;

            account!.SeedState(
                activeStatementId,
                command.ActiveStatementBalance,
                command.ActiveStatementMinimumPaymentDue,
                command.ActiveStatementPeriodCloseDate,
                command.ActiveStatementDueDate,
                command.UnbilledBalance,
                DateTimeOffset.UtcNow);

            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailed(context, command.CorrelationId, command.UserId, ex.Message, ct);
        }
    }

    private static Task PublishFailed(
        ConsumeContext<SeedCreditCardState> context,
        Guid correlationId,
        string userId,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            correlationId,
            userId,
            nameof(SeedCreditCardState),
            reason,
            DateTimeOffset.UtcNow), ct);
}

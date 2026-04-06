using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class PostFundingTransferHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<PostFundingTransfer>
{
    public async Task Consume(ConsumeContext<PostFundingTransfer> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;

        try
        {
            var account = await repository.LoadAsync(new FundingAccountId(command.FundingAccountId), ct);

            var guard = CommandGuard.Ok
                .Require(() => account is not null, "Funding account not found.")
                .Require(() => account!.UserId == new UserId(command.UserId), "Access denied.");

            if (guard.HasFailed(out var reason))
            {
                await PublishFailureAsync(context, command, reason, ct);
                return;
            }

            account!.PostTransfer(
                new TransferId(command.TransferId),
                command.Amount,
                command.PostedAtUtc,
                command.TargetCreditCardAccountId is null ? null : new CreditCardAccountId(command.TargetCreditCardAccountId),
                command.StatementId is null ? null : new CreditCardStatementId(command.StatementId));

            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailureAsync(context, command, ex.Message, ct);
        }
    }

    private static Task PublishFailureAsync(
        ConsumeContext<PostFundingTransfer> context,
        PostFundingTransfer command,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            command.CorrelationId,
            command.UserId,
            nameof(PostFundingTransfer),
            reason,
            DateTimeOffset.UtcNow), ct);
}

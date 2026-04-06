using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class RemoveFundingPaymentInstrumentHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<RemoveFundingPaymentInstrument>
{
    public async Task Consume(ConsumeContext<RemoveFundingPaymentInstrument> context)
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

            account!.RemovePaymentInstrument(new PaymentInstrumentId(command.PaymentInstrumentId));
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailureAsync(context, command, ex.Message, ct);
        }
    }

    private static Task PublishFailureAsync(
        ConsumeContext<RemoveFundingPaymentInstrument> context,
        RemoveFundingPaymentInstrument command,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            command.CorrelationId,
            command.UserId,
            nameof(RemoveFundingPaymentInstrument),
            reason,
            DateTimeOffset.UtcNow), ct);
}

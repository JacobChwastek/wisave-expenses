using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Framework.Application;
using WiSave.Framework.Domain;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class UpdateFundingPaymentInstrumentHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<UpdateFundingPaymentInstrument>
{
    public async Task Consume(ConsumeContext<UpdateFundingPaymentInstrument> context)
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

            account!.UpdatePaymentInstrument(
                new PaymentInstrumentId(command.PaymentInstrumentId),
                command.Kind,
                command.Name,
                command.LastFourDigits,
                command.Network,
                command.Color);
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailureAsync(context, command, ex.Message, ct);
        }
    }

    private static Task PublishFailureAsync(
        ConsumeContext<UpdateFundingPaymentInstrument> context,
        UpdateFundingPaymentInstrument command,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            command.CorrelationId,
            command.UserId,
            nameof(UpdateFundingPaymentInstrument),
            reason,
            DateTimeOffset.UtcNow), ct);
}

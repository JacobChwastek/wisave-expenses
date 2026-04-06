using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class UpdateFundingAccountHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<UpdateFundingAccount>
{
    public async Task Consume(ConsumeContext<UpdateFundingAccount> context)
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
                await context.Publish(new CommandFailed(
                    command.CorrelationId,
                    command.UserId,
                    nameof(UpdateFundingAccount),
                    reason,
                    DateTimeOffset.UtcNow), ct);
                return;
            }

            account!.Reconfigure(command.Name, command.Kind, command.Currency, command.Color);
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId,
                command.UserId,
                nameof(UpdateFundingAccount),
                ex.Message,
                DateTimeOffset.UtcNow), ct);
        }
    }
}

using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Framework.Application;
using WiSave.Framework.Domain;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class CloseFundingAccountHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<CloseFundingAccount>
{
    public async Task Consume(ConsumeContext<CloseFundingAccount> context)
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
                    nameof(CloseFundingAccount),
                    reason,
                    DateTimeOffset.UtcNow), ct);
                return;
            }

            account!.Close();
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId,
                command.UserId,
                nameof(CloseFundingAccount),
                ex.Message,
                DateTimeOffset.UtcNow), ct);
        }
    }
}

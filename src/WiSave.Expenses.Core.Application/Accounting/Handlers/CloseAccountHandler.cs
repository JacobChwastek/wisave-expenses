using MassTransit;
using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class CloseAccountHandler(IAggregateRepository<Account> repository) : IConsumer<CloseAccount>
{
    public async Task Consume(ConsumeContext<CloseAccount> context)
    {
        var command = context.Message;
        try
        {
            var account = await repository.LoadAsync($"account-{command.AccountId}", context.CancellationToken);
            if (account is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(CloseAccount), "Account not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (account.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(CloseAccount), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            account.Close();
            await repository.SaveAsync(account, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(CloseAccount), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

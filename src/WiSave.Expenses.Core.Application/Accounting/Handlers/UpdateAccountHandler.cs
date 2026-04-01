using MassTransit;
using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class UpdateAccountHandler(IAggregateRepository<Account> repository) : IConsumer<UpdateAccount>
{
    public async Task Consume(ConsumeContext<UpdateAccount> context)
    {
        var command = context.Message;
        try
        {
            var account = await repository.LoadAsync($"account-{command.AccountId}", context.CancellationToken);
            if (account is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(UpdateAccount), "Account not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (account.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(UpdateAccount), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            account.Update(
                command.Name,
                command.Type,
                command.Currency,
                command.Balance,
                command.LinkedBankAccountId is not null ? new AccountId(command.LinkedBankAccountId) : null,
                command.CreditLimit,
                command.BillingCycleDay,
                command.Color,
                command.LastFourDigits);

            await repository.SaveAsync(account, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(UpdateAccount), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

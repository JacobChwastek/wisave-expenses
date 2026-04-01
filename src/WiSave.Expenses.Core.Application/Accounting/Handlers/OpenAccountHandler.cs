using MassTransit;
using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class OpenAccountHandler(IAggregateRepository<Account> repository) : IConsumer<OpenAccount>
{
    public async Task Consume(ConsumeContext<OpenAccount> context)
    {
        var command = context.Message;
        try
        {
            var accountId = Guid.NewGuid().ToString();
            var account = Account.Open(
                new AccountId(accountId), new UserId(command.UserId), command.Name,
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
                command.CorrelationId, command.UserId, nameof(OpenAccount), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

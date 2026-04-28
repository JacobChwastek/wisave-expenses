using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Framework.Application;
using WiSave.Framework.Domain;

namespace WiSave.Expenses.Core.Application.Funding.Handlers;

public sealed class OpenFundingAccountHandler(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IConsumer<OpenFundingAccount>
{
    public async Task Consume(ConsumeContext<OpenFundingAccount> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;

        try
        {
            var accountId = Guid.NewGuid().ToString();
            var account = FundingAccount.Open(
                new FundingAccountId(accountId),
                new UserId(command.UserId.ToString()),
                command.Name,
                command.Kind,
                command.Currency,
                command.OpeningBalance,
                command.Color);

            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId,
                command.UserId.ToString(),
                nameof(OpenFundingAccount),
                ex.Message,
                DateTimeOffset.UtcNow), ct);
        }
    }
}

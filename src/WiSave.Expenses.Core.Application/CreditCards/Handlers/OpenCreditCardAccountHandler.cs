using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class OpenCreditCardAccountHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository,
    IFundingAccountLookup fundingAccountLookup) : IConsumer<OpenCreditCardAccount>
{
    public async Task Consume(ConsumeContext<OpenCreditCardAccount> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;
        var userId = command.UserId.ToString();

        try
        {
            var guard = await ValidateSettlementAccount(command.SettlementAccountId, userId, command.Currency, ct);
            if (guard.HasFailed(out var reason))
            {
                await PublishFailed(context, command.CorrelationId, userId, reason, ct);
                return;
            }

            var account = CreditCardAccount.Open(
                new CreditCardAccountId(Guid.NewGuid().ToString()),
                new UserId(userId),
                command.Name,
                command.Currency,
                new FundingAccountId(command.SettlementAccountId),
                command.BankProvider,
                command.ProductCode,
                command.CreditLimit,
                new StatementClosingDay(command.StatementClosingDay),
                new GracePeriodDays(command.GracePeriodDays),
                command.Color,
                command.LastFourDigits,
                DateTimeOffset.UtcNow);

            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailed(context, command.CorrelationId, userId, ex.Message, ct);
        }
    }

    private async Task<CommandGuard> ValidateSettlementAccount(
        string settlementAccountId,
        string userId,
        Currency currency,
        CancellationToken ct)
    {
        var account = await fundingAccountLookup.GetAsync(settlementAccountId, ct);

        return CommandGuard.Ok
            .Require(() => account is not null, "Settlement funding account not found.")
            .Require(() => account!.UserId == userId, "Settlement funding account access denied.")
            .Require(() => account!.IsActive, "Settlement funding account is inactive.")
            .Require(() => account!.Currency == currency, "Settlement funding account currency mismatch.");
    }

    private static Task PublishFailed(
        ConsumeContext<OpenCreditCardAccount> context,
        Guid correlationId,
        string userId,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            correlationId,
            userId,
            nameof(OpenCreditCardAccount),
            reason,
            DateTimeOffset.UtcNow), ct);
}

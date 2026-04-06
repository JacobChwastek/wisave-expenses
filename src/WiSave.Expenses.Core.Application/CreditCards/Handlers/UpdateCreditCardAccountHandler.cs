using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class UpdateCreditCardAccountHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository,
    IFundingAccountLookup fundingAccountLookup) : IConsumer<UpdateCreditCardAccount>
{
    public async Task Consume(ConsumeContext<UpdateCreditCardAccount> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;

        try
        {
            var account = await repository.LoadAsync(new CreditCardAccountId(command.CreditCardAccountId), ct);
            var guard = await Validate(account, command, ct);

            if (guard.HasFailed(out var reason))
            {
                await PublishFailed(context, command.CorrelationId, command.UserId, reason, ct);
                return;
            }

            account!.Reconfigure(
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
            await PublishFailed(context, command.CorrelationId, command.UserId, ex.Message, ct);
        }
    }

    private async Task<CommandGuard> Validate(CreditCardAccount? account, UpdateCreditCardAccount command, CancellationToken ct)
    {
        var settlementAccount = await fundingAccountLookup.GetAsync(command.SettlementAccountId, ct);

        return CommandGuard.Ok
            .Require(() => account is not null, "Credit card account not found.")
            .Require(() => account!.UserId == new UserId(command.UserId), "Access denied.")
            .Require(() => settlementAccount is not null, "Settlement funding account not found.")
            .Require(() => settlementAccount!.UserId == command.UserId, "Settlement funding account access denied.")
            .Require(() => settlementAccount!.IsActive, "Settlement funding account is inactive.")
            .Require(() => settlementAccount!.Currency == command.Currency, "Settlement funding account currency mismatch.");
    }

    private static Task PublishFailed(
        ConsumeContext<UpdateCreditCardAccount> context,
        Guid correlationId,
        string userId,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            correlationId,
            userId,
            nameof(UpdateCreditCardAccount),
            reason,
            DateTimeOffset.UtcNow), ct);
}

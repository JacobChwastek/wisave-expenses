using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class IssueCreditCardStatementHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository,
    ICreditCardStatementPolicyResolver policyResolver) : IConsumer<IssueCreditCardStatement>
{
    public async Task Consume(ConsumeContext<IssueCreditCardStatement> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;

        try
        {
            var account = await repository.LoadAsync(new CreditCardAccountId(command.CreditCardAccountId), ct);
            var guard = CommandGuard.Ok
                .Require(() => account is not null, "Credit card account not found.")
                .Require(() => account!.UserId == new UserId(command.UserId), "Access denied.");

            if (guard.HasFailed(out var reason))
            {
                await PublishFailed(context, command.CorrelationId, command.UserId, reason, ct);
                return;
            }

            var policy = policyResolver.Resolve(account!.BankProvider, account.ProductCode);
            var computation = policy.Compute(new CreditCardStatementPolicyContext(
                account.Id,
                account.Currency,
                account.CreditLimit,
                account.UnbilledBalance,
                account.StatementClosingDay,
                account.GracePeriodDays,
                command.CalculationDate));

            account.IssueStatement(
                new CreditCardStatementId($"stmt-{Guid.NewGuid():N}"),
                computation,
                DateTimeOffset.UtcNow);
            await repository.SaveAsync(account, ct);
        }
        catch (DomainException ex)
        {
            await PublishFailed(context, command.CorrelationId, command.UserId, ex.Message, ct);
        }
    }

    private static Task PublishFailed(
        ConsumeContext<IssueCreditCardStatement> context,
        Guid correlationId,
        string userId,
        string reason,
        CancellationToken ct) =>
        context.Publish(new CommandFailed(
            correlationId,
            userId,
            nameof(IssueCreditCardStatement),
            reason,
            DateTimeOffset.UtcNow), ct);
}

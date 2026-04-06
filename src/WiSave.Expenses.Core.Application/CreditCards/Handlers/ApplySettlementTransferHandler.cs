using MassTransit;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;

namespace WiSave.Expenses.Core.Application.CreditCards.Handlers;

public sealed class ApplySettlementTransferHandler(
    IAggregateRepository<CreditCardAccount, CreditCardAccountId> repository,
    ICreditCardPaymentAllocationPolicy allocationPolicy) : IConsumer<FundingTransferPosted>
{
    public async Task Consume(ConsumeContext<FundingTransferPosted> context)
    {
        var message = context.Message;
        if (message.TargetCreditCardAccountId is null)
            return;

        var account = await repository.LoadAsync(new CreditCardAccountId(message.TargetCreditCardAccountId), context.CancellationToken);
        if (account is null)
            return;

        if (account.UserId.Value != message.UserId || account.SettlementAccountId.Value != message.FundingAccountId)
            return;

        var openStatements = account.GetOpenStatementSnapshots();
        var decisions = BuildAllocationDecisions(message, openStatements);
        if (decisions.Count == 0)
            return;

        try
        {
            account.ApplySettlementTransfer(new TransferId(message.TransferId), message.Amount, message.Timestamp, decisions);
            await repository.SaveAsync(account, context.CancellationToken);
        }
        catch (SettlementTransferAlreadyAppliedWithDifferentAllocationsException)
        {
            return;
        }
    }

    private IReadOnlyCollection<CreditCardPaymentAllocationDecision> BuildAllocationDecisions(
        FundingTransferPosted message,
        IReadOnlyCollection<OpenStatementSnapshot> openStatements)
    {
        if (message.StatementId is null)
            return allocationPolicy.Allocate(message.Amount, openStatements);

        var targetStatement = openStatements.SingleOrDefault(x => x.StatementId == message.StatementId);
        if (targetStatement is null)
            return [];

        var amount = Math.Min(message.Amount, targetStatement.OutstandingBalance);
        return amount <= 0m
            ? []
            : [new CreditCardPaymentAllocationDecision(targetStatement.StatementId, amount)];
    }
}

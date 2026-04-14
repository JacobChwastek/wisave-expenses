using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class CopyBudgetFromPreviousHandler(IAggregateRepository<Budget> repository) : IConsumer<CopyBudgetFromPrevious>
{
    public async Task Consume(ConsumeContext<CopyBudgetFromPrevious> context)
    {
        var command = context.Message;
        try
        {
            var sourceMonth = command.Month == 1 ? 12 : command.Month - 1;
            var sourceYear = command.Month == 1 ? command.Year - 1 : command.Year;

            var sourceStreamId = $"budget-{command.UserId}-{sourceYear}-{sourceMonth:D2}";
            var sourceBudget = await repository.LoadAsync(sourceStreamId, context.CancellationToken);

            var guard = CommandGuard.Ok
                .Require(() => sourceBudget is not null, "No previous month budget found to copy from.")
                .Require(() => sourceBudget!.Recurring, "Previous month budget is not marked as recurring.");

            if (guard.HasFailed(out var reason))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(CopyBudgetFromPrevious), reason, DateTimeOffset.UtcNow));
                return;
            }

            var budgetId = $"{command.UserId}-{command.Year}-{command.Month:D2}";
            var newBudget = Budget.CopyFromPrevious(
                new BudgetId(budgetId), new UserId(command.UserId), command.Month, command.Year,
                sourceMonth, sourceYear,
                sourceBudget!.Currency, sourceBudget.TotalLimit,
                sourceBudget.Recurring, sourceBudget.CategoryBudgets.ToDictionary(cb => cb.CategoryId, cb => cb.Limit));

            await repository.SaveAsync(newBudget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(CopyBudgetFromPrevious), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

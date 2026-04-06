using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class RemoveCategoryLimitHandler(IAggregateRepository<Budget, BudgetId> repository) : IConsumer<RemoveCategoryLimit>
{
    public async Task Consume(ConsumeContext<RemoveCategoryLimit> context)
    {
        var command = context.Message;
        try
        {
            var budget = await repository.LoadAsync(new BudgetId(command.BudgetId), context.CancellationToken);

            var guard = CommandGuard.Ok
                .Require(() => budget is not null, "Budget not found.")
                .Require(() => budget!.UserId == new UserId(command.UserId), "Access denied.");

            if (guard.HasFailed(out var reason))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RemoveCategoryLimit), reason, DateTimeOffset.UtcNow));
                return;
            }

            budget!.RemoveCategoryLimit(new CategoryId(command.CategoryId));
            await repository.SaveAsync(budget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(RemoveCategoryLimit), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

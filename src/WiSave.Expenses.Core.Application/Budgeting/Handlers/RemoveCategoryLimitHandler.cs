using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class RemoveCategoryLimitHandler(IAggregateRepository<Budget> repository) : IConsumer<RemoveCategoryLimit>
{
    public async Task Consume(ConsumeContext<RemoveCategoryLimit> context)
    {
        var command = context.Message;
        try
        {
            var budget = await repository.LoadAsync($"budget-{command.BudgetId}", context.CancellationToken);
            if (budget is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RemoveCategoryLimit), "Budget not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (budget.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RemoveCategoryLimit), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            budget.RemoveCategoryLimit(new CategoryId(command.CategoryId));
            await repository.SaveAsync(budget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(RemoveCategoryLimit), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

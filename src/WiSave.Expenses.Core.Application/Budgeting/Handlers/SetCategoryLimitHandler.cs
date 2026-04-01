using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class SetCategoryLimitHandler(
    IAggregateRepository<Budget> repository,
    ICategoryRepository categoryRepository) : IConsumer<SetCategoryLimit>
{
    public async Task Consume(ConsumeContext<SetCategoryLimit> context)
    {
        var command = context.Message;
        try
        {
            var budget = await repository.LoadAsync($"budget-{command.BudgetId}", context.CancellationToken);
            if (budget is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetCategoryLimit), "Budget not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (budget.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetCategoryLimit), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            if (!await categoryRepository.ExistsAsync(command.CategoryId, command.UserId, context.CancellationToken))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetCategoryLimit), "Category not found.", DateTimeOffset.UtcNow));
                return;
            }

            budget.SetCategoryLimit(new CategoryId(command.CategoryId), command.Limit);
            await repository.SaveAsync(budget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(SetCategoryLimit), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

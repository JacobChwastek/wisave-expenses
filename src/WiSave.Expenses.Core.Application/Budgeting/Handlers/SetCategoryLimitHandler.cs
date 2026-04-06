using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class SetCategoryLimitHandler(
    IAggregateRepository<Budget, BudgetId> repository,
    ICategoryRepository categoryRepository) : IConsumer<SetCategoryLimit>
{
    public async Task Consume(ConsumeContext<SetCategoryLimit> context)
    {
        var command = context.Message;
        var ct = context.CancellationToken;
        try
        {
            var budget = await repository.LoadAsync(new BudgetId(command.BudgetId), ct);

            var guard = await CommandGuard.Ok
                .Require(() => budget is not null, "Budget not found.")
                .Require(() => budget!.UserId == new UserId(command.UserId), "Access denied.")
                .RequireAsync(() => categoryRepository.ExistsAsync(command.CategoryId, command.UserId, ct), "Category not found.");

            if (guard.HasFailed(out var reason))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetCategoryLimit), reason, DateTimeOffset.UtcNow), ct);
                return;
            }

            budget!.SetCategoryLimit(new CategoryId(command.CategoryId), command.Limit);
            await repository.SaveAsync(budget, ct);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(SetCategoryLimit), ex.Message, DateTimeOffset.UtcNow), ct);
        }
    }
}

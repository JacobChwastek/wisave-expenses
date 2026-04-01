using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class SetOverallLimitHandler(IAggregateRepository<Budget> repository) : IConsumer<SetOverallLimit>
{
    public async Task Consume(ConsumeContext<SetOverallLimit> context)
    {
        var command = context.Message;
        try
        {
            var budget = await repository.LoadAsync($"budget-{command.BudgetId}", context.CancellationToken);
            if (budget is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetOverallLimit), "Budget not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (budget.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(SetOverallLimit), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            budget.SetOverallLimit(command.TotalLimit);
            await repository.SaveAsync(budget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(SetOverallLimit), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

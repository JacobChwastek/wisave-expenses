using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class CreateBudgetHandler(IAggregateRepository<Budget> repository) : IConsumer<CreateBudget>
{
    public async Task Consume(ConsumeContext<CreateBudget> context)
    {
        var command = context.Message;
        try
        {
            var budgetId = $"{command.UserId}-{command.Year}-{command.Month:D2}";
            var budget = Budget.Create(
                new BudgetId(budgetId), new UserId(command.UserId), command.Month, command.Year,
                command.TotalLimit, command.Currency, command.Recurring);

            await repository.SaveAsync(budget, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(CreateBudget), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

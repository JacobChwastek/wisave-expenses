using MassTransit;
using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class UpdateExpenseHandler(IAggregateRepository<Expense> repository) : IConsumer<UpdateExpense>
{
    public async Task Consume(ConsumeContext<UpdateExpense> context)
    {
        var command = context.Message;
        try
        {
            var expense = await repository.LoadAsync($"expense-{command.ExpenseId}", context.CancellationToken);
            if (expense is null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(UpdateExpense), "Expense not found.", DateTimeOffset.UtcNow));
                return;
            }
            if (expense.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(UpdateExpense), "Access denied.", DateTimeOffset.UtcNow));
                return;
            }

            expense.Update(
                command.Amount,
                command.Currency,
                command.Date,
                command.Description,
                command.CategoryId is not null ? new CategoryId(command.CategoryId) : null,
                command.SubcategoryId is not null ? new SubcategoryId(command.SubcategoryId) : null,
                command.Recurring,
                command.Metadata);

            await repository.SaveAsync(expense, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(UpdateExpense), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

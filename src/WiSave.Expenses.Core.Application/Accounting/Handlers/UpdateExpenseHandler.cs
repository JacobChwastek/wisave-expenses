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

            var guard = CommandGuard.Ok
                .Require(() => expense is not null, "Expense not found.")
                .Require(() => expense!.UserId == new UserId(command.UserId), "Access denied.");

            if (guard.HasFailed(out var reason))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(UpdateExpense), reason, DateTimeOffset.UtcNow));
                return;
            }

            expense!.Update(
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

using MassTransit;
using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class DeleteExpenseHandler(IAggregateRepository<Expense> repository) : IConsumer<DeleteExpense>
{
    public async Task Consume(ConsumeContext<DeleteExpense> context)
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
                    command.CorrelationId, command.UserId, nameof(DeleteExpense), reason, DateTimeOffset.UtcNow));
                return;
            }

            expense!.Delete();
            await repository.SaveAsync(expense, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(DeleteExpense), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

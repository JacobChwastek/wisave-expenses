using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class DeleteExpenseHandler(IAggregateRepository<Expense> repository)
{
    public async Task<CommandResult> HandleAsync(DeleteExpense command, CancellationToken ct = default)
    {
        try
        {
            var expense = await repository.LoadAsync($"expense-{command.ExpenseId}", ct);
            if (expense is null)
                return CommandResult.Failure("Expense not found.");
            if (expense.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Access denied.");

            expense.Delete();
            await repository.SaveAsync(expense, ct);
            return CommandResult.Success(command.ExpenseId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

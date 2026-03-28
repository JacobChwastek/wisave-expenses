using WiSave.Expenses.Contracts.Commands;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class UpdateExpenseHandler(IAggregateRepository<Expense> repository)
{
    public async Task<CommandResult> HandleAsync(UpdateExpense command, CancellationToken ct = default)
    {
        try
        {
            var expense = await repository.LoadAsync($"expense-{command.ExpenseId}", ct);
            if (expense is null)
                return CommandResult.Failure("Expense not found.");
            if (expense.UserId != command.UserId)
                return CommandResult.Failure("Access denied.");

            expense.Update(
                command.Amount,
                command.Currency?.ToString(),
                command.Date,
                command.Description,
                command.CategoryId,
                command.SubcategoryId,
                command.Recurring,
                command.Metadata);

            await repository.SaveAsync(expense, ct);
            return CommandResult.Success(command.ExpenseId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

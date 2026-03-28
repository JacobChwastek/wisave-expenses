using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class RecordExpenseHandler(
    IAggregateRepository<Expense> expenseRepository,
    IAggregateRepository<Account> accountRepository,
    ICategoryRepository categoryRepository)
{
    public async Task<CommandResult> HandleAsync(RecordExpense command, CancellationToken ct = default)
    {
        try
        {
            var account = await accountRepository.LoadAsync($"account-{command.AccountId}", ct);
            if (account is null || account.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Account not found or access denied.");
            if (!account.IsActive)
                return CommandResult.Failure("Cannot record expense on a closed account.");

            if (!await categoryRepository.ExistsAsync(command.CategoryId, command.UserId, ct))
                return CommandResult.Failure("Category not found.");

            if (command.SubcategoryId is not null &&
                !await categoryRepository.SubcategoryExistsAsync(command.SubcategoryId, command.CategoryId, ct))
                return CommandResult.Failure("Subcategory not found.");

            var expenseId = Guid.NewGuid().ToString();
            var expense = Expense.Record(
                new ExpenseId(expenseId), new UserId(command.UserId), new AccountId(command.AccountId), new CategoryId(command.CategoryId),
                command.SubcategoryId is not null ? new SubcategoryId(command.SubcategoryId) : null, command.Amount, command.Currency,
                command.Date, command.Description, command.Recurring,
                command.Metadata);

            await expenseRepository.SaveAsync(expense, ct);
            return CommandResult.Success(expenseId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

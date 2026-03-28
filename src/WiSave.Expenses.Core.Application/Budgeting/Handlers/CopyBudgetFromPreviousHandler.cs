using WiSave.Expenses.Contracts.Commands;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class CopyBudgetFromPreviousHandler(
    IAggregateRepository<Budget> repository,
    IBudgetUniquenessChecker uniquenessChecker)
{
    public async Task<CommandResult> HandleAsync(CopyBudgetFromPrevious command, CancellationToken ct = default)
    {
        try
        {
            if (await uniquenessChecker.ExistsAsync(command.UserId, command.Month, command.Year, ct))
                return CommandResult.Failure($"Budget for {command.Month}/{command.Year} already exists.");

            var sourceMonth = command.Month == 1 ? 12 : command.Month - 1;
            var sourceYear = command.Month == 1 ? command.Year - 1 : command.Year;

            // Find source budget by scanning known stream name pattern
            // In practice, the uniqueness checker table can provide the source budgetId
            // For now, we use a convention-based stream lookup
            var sourceBudget = await FindBudgetForMonthAsync(command.UserId, sourceMonth, sourceYear, ct);
            if (sourceBudget is null)
                return CommandResult.Failure("No previous month budget found to copy from.");
            if (!sourceBudget.Recurring)
                return CommandResult.Failure("Previous month budget is not marked as recurring.");

            var budgetId = Guid.NewGuid().ToString();
            var newBudget = Budget.CopyFromPrevious(
                new BudgetId(budgetId), new UserId(command.UserId), command.Month, command.Year,
                sourceMonth, sourceYear,
                sourceBudget.Currency, sourceBudget.TotalLimit,
                sourceBudget.Recurring, sourceBudget.CategoryBudgets.ToDictionary(cb => cb.CategoryId, cb => cb.Limit));

            await uniquenessChecker.ReserveAsync(budgetId, command.UserId, command.Month, command.Year, ct);
            await repository.SaveAsync(newBudget, ct);
            return CommandResult.Success(budgetId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }

    private async Task<Budget?> FindBudgetForMonthAsync(string userId, int month, int year, CancellationToken ct)
    {
        // TODO: In infrastructure, implement a lookup from the uniqueness table to get the budgetId
        // then load the aggregate by stream name. For now this is a placeholder.
        _ = userId;
        _ = month;
        _ = year;
        _ = ct;
        return null;
    }
}

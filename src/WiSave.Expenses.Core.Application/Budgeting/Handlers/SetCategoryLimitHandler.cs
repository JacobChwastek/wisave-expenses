using WiSave.Expenses.Contracts.Commands;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class SetCategoryLimitHandler(
    IAggregateRepository<Budget> repository,
    ICategoryRepository categoryRepository)
{
    public async Task<CommandResult> HandleAsync(SetCategoryLimit command, CancellationToken ct = default)
    {
        try
        {
            var budget = await repository.LoadAsync($"budget-{command.BudgetId}", ct);
            if (budget is null)
                return CommandResult.Failure("Budget not found.");
            if (budget.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Access denied.");

            if (!await categoryRepository.ExistsAsync(command.CategoryId, command.UserId, ct))
                return CommandResult.Failure("Category not found.");

            budget.SetCategoryLimit(new CategoryId(command.CategoryId), command.Limit);
            await repository.SaveAsync(budget, ct);
            return CommandResult.Success(command.BudgetId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

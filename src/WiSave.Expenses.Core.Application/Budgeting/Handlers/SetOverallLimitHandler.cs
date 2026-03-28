using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class SetOverallLimitHandler(IAggregateRepository<Budget> repository)
{
    public async Task<CommandResult> HandleAsync(SetOverallLimit command, CancellationToken ct = default)
    {
        try
        {
            var budget = await repository.LoadAsync($"budget-{command.BudgetId}", ct);
            if (budget is null)
                return CommandResult.Failure("Budget not found.");
            if (budget.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Access denied.");

            budget.SetOverallLimit(command.TotalLimit);
            await repository.SaveAsync(budget, ct);
            return CommandResult.Success(command.BudgetId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

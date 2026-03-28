using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Budgeting.Handlers;

public sealed class CreateBudgetHandler(
    IAggregateRepository<Budget> repository,
    IBudgetUniquenessChecker uniquenessChecker)
{
    public async Task<CommandResult> HandleAsync(CreateBudget command, CancellationToken ct = default)
    {
        try
        {
            if (await uniquenessChecker.ExistsAsync(command.UserId, command.Month, command.Year, ct))
                return CommandResult.Failure($"Budget for {command.Month}/{command.Year} already exists.");

            var budgetId = Guid.NewGuid().ToString();
            var budget = Budget.Create(
                new BudgetId(budgetId), new UserId(command.UserId), command.Month, command.Year,
                command.TotalLimit, command.Currency, command.Recurring);

            await uniquenessChecker.ReserveAsync(budgetId, command.UserId, command.Month, command.Year, ct);
            await repository.SaveAsync(budget, ct);
            return CommandResult.Success(budgetId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

using WiSave.Expenses.Contracts.Commands;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class CloseAccountHandler(IAggregateRepository<Account> repository)
{
    public async Task<CommandResult> HandleAsync(CloseAccount command, CancellationToken ct = default)
    {
        try
        {
            var account = await repository.LoadAsync($"account-{command.AccountId}", ct);
            if (account is null)
                return CommandResult.Failure("Account not found.");
            if (account.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Access denied.");

            account.Close();
            await repository.SaveAsync(account, ct);
            return CommandResult.Success(command.AccountId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class UpdateAccountHandler(IAggregateRepository<Account> repository)
{
    public async Task<CommandResult> HandleAsync(UpdateAccount command, CancellationToken ct = default)
    {
        try
        {
            var account = await repository.LoadAsync($"account-{command.AccountId}", ct);
            if (account is null)
                return CommandResult.Failure("Account not found.");
            if (account.UserId != new UserId(command.UserId))
                return CommandResult.Failure("Access denied.");

            account.Update(
                command.Name,
                command.Type,
                command.Currency,
                command.Balance,
                command.LinkedBankAccountId is not null ? new AccountId(command.LinkedBankAccountId) : null,
                command.CreditLimit,
                command.BillingCycleDay,
                command.Color,
                command.LastFourDigits);

            await repository.SaveAsync(account, ct);
            return CommandResult.Success(command.AccountId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

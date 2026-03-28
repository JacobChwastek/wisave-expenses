using WiSave.Expenses.Contracts.Commands;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class OpenAccountHandler(IAggregateRepository<Account> repository)
{
    public async Task<CommandResult> HandleAsync(OpenAccount command, CancellationToken ct = default)
    {
        try
        {
            var accountId = Guid.NewGuid().ToString();
            var account = Account.Open(
                accountId, command.UserId, command.Name,
                command.Type.ToString().ToLowerInvariant(),
                command.Currency.ToString(),
                command.Balance,
                command.LinkedBankAccountId,
                command.CreditLimit,
                command.BillingCycleDay,
                command.Color,
                command.LastFourDigits);

            await repository.SaveAsync(account, ct);
            return CommandResult.Success(accountId);
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}

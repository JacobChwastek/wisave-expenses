using MassTransit;
using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Application.Accounting.Handlers;

public sealed class RecordExpenseHandler(
    IAggregateRepository<Expense> expenseRepository,
    IAggregateRepository<Account> accountRepository,
    ICategoryRepository categoryRepository) : IConsumer<RecordExpense>
{
    public async Task Consume(ConsumeContext<RecordExpense> context)
    {
        var command = context.Message;
        try
        {
            var account = await accountRepository.LoadAsync($"account-{command.AccountId}", context.CancellationToken);
            if (account is null || account.UserId != new UserId(command.UserId))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RecordExpense), "Account not found or access denied.", DateTimeOffset.UtcNow));
                return;
            }
            if (!account.IsActive)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RecordExpense), "Cannot record expense on a closed account.", DateTimeOffset.UtcNow));
                return;
            }

            if (!await categoryRepository.ExistsAsync(command.CategoryId, command.UserId, context.CancellationToken))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RecordExpense), "Category not found.", DateTimeOffset.UtcNow));
                return;
            }

            if (command.SubcategoryId is not null &&
                !await categoryRepository.SubcategoryExistsAsync(command.SubcategoryId, command.CategoryId, context.CancellationToken))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RecordExpense), "Subcategory not found.", DateTimeOffset.UtcNow));
                return;
            }

            var expenseId = Guid.NewGuid().ToString();
            var expense = Expense.Record(
                new ExpenseId(expenseId), new UserId(command.UserId), new AccountId(command.AccountId), new CategoryId(command.CategoryId),
                command.SubcategoryId is not null ? new SubcategoryId(command.SubcategoryId) : null, command.Amount, command.Currency,
                command.Date, command.Description, command.Recurring,
                command.Metadata);

            await expenseRepository.SaveAsync(expense, context.CancellationToken);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(RecordExpense), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}

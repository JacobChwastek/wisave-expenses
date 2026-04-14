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
        var ct = context.CancellationToken;
        try
        {
            var account = await accountRepository.LoadAsync($"account-{command.AccountId}", ct);

            var guard = await CommandGuard.Ok
                .Require(() => account is not null, "Account not found or access denied.")
                .Require(() => account!.UserId == new UserId(command.UserId), "Access denied.")
                .Require(() => account!.IsActive, "Cannot record expense on a closed account.")
                .RequireAsync(() => categoryRepository.ExistsAsync(command.CategoryId, command.UserId, ct), "Category not found.")
                .RequireAsync(
                    () => command.SubcategoryId is null
                        ? Task.FromResult(true)
                        : categoryRepository.SubcategoryExistsAsync(command.SubcategoryId, command.CategoryId, ct),
                    "Subcategory not found.");

            if (guard.HasFailed(out var reason))
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId, nameof(RecordExpense), reason, DateTimeOffset.UtcNow), ct);
                return;
            }

            var expenseId = Guid.NewGuid().ToString();
            var expense = Expense.Record(
                new ExpenseId(expenseId), new UserId(command.UserId), new AccountId(command.AccountId), new CategoryId(command.CategoryId),
                command.SubcategoryId is not null ? new SubcategoryId(command.SubcategoryId) : null, command.Amount, command.Currency,
                command.Date, command.Description, command.Recurring,
                command.Metadata);

            await expenseRepository.SaveAsync(expense, ct);
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId, nameof(RecordExpense), ex.Message, DateTimeOffset.UtcNow), ct);
        }
    }
}

using WiSave.Expenses.Core.Domain.Accounting.Events;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Accounting;

public sealed class Expense : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string AccountId { get; private set; } = string.Empty;
    public string CategoryId { get; private set; } = string.Empty;
    public string? SubcategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool Recurring { get; private set; }
    public Dictionary<string, string>? Metadata { get; private set; }
    public bool IsDeleted { get; private set; }

    public Expense() { }

    public static Expense Record(
        string id, string userId, string accountId, string categoryId, string? subcategoryId,
        decimal amount, string currency, DateOnly date, string description,
        bool recurring = false, Dictionary<string, string>? metadata = null)
    {
        if (amount <= 0)
            throw new DomainException("Expense amount must be positive.");

        var expense = new Expense();
        expense.RaiseEvent(new ExpenseRecordedEvent(
            id, userId, accountId, categoryId, subcategoryId,
            amount, currency, date, description, recurring, metadata));
        return expense;
    }

    public void Update(
        decimal? amount = null, string? currency = null, DateOnly? date = null,
        string? description = null, string? categoryId = null, string? subcategoryId = null,
        bool? recurring = null, Dictionary<string, string>? metadata = null)
    {
        EnsureNotDeleted();

        if (amount.HasValue && amount.Value <= 0)
            throw new DomainException("Expense amount must be positive.");

        RaiseEvent(new ExpenseUpdatedEvent(
            Id, amount, currency, date, description, categoryId, subcategoryId, recurring, metadata));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        RaiseEvent(new ExpenseDeletedEvent(Id));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify a deleted expense.");
    }

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case ExpenseRecordedEvent e:
                Id = e.ExpenseId;
                UserId = e.UserId;
                AccountId = e.AccountId;
                CategoryId = e.CategoryId;
                SubcategoryId = e.SubcategoryId;
                Amount = e.Amount;
                Currency = e.Currency;
                Date = e.Date;
                Description = e.Description;
                Recurring = e.Recurring;
                Metadata = e.Metadata;
                IsDeleted = false;
                break;

            case ExpenseUpdatedEvent e:
                if (e.Amount.HasValue) Amount = e.Amount.Value;
                if (e.Currency is not null) Currency = e.Currency;
                if (e.Date.HasValue) Date = e.Date.Value;
                if (e.Description is not null) Description = e.Description;
                if (e.CategoryId is not null) CategoryId = e.CategoryId;
                if (e.SubcategoryId is not null) SubcategoryId = e.SubcategoryId;
                if (e.Recurring.HasValue) Recurring = e.Recurring.Value;
                if (e.Metadata is not null) Metadata = e.Metadata;
                break;

            case ExpenseDeletedEvent:
                IsDeleted = true;
                break;
        }
    }
}

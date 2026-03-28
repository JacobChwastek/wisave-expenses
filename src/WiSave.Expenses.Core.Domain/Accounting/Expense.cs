using WiSave.Expenses.Contracts.Events.Expenses;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Accounting;

public sealed class Expense : AggregateRoot
{
    public UserId UserId { get; private set; } = null!;
    public AccountId AccountId { get; private set; } = null!;
    public CategoryId CategoryId { get; private set; } = null!;
    public SubcategoryId? SubcategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool Recurring { get; private set; }
    public Dictionary<string, string>? Metadata { get; private set; }
    public bool IsDeleted { get; private set; }

    public Expense() { }

    public static Expense Record(
        ExpenseId id, UserId userId, AccountId accountId, CategoryId categoryId, SubcategoryId? subcategoryId,
        decimal amount, Currency currency, DateOnly date, string description,
        bool recurring = false, Dictionary<string, string>? metadata = null)
    {
        if (amount <= 0)
            throw new DomainException("Expense amount must be positive.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Expense description is required.");

        var expense = new Expense();
        expense.RaiseEvent(new ExpenseRecorded(
            id.Value, userId.Value, accountId.Value, categoryId.Value, subcategoryId?.Value,
            amount, currency, date, description, recurring, metadata,
            DateTimeOffset.UtcNow));
        return expense;
    }

    public void ChangeAmount(decimal amount, Currency currency)
    {
        EnsureNotDeleted();
        if (amount <= 0)
            throw new DomainException("Expense amount must be positive.");

        RaiseEvent(new ExpenseUpdated(
            Id, UserId.Value, amount, currency, null, null, null, null, null, null,
            DateTimeOffset.UtcNow));
    }

    public void Recategorize(CategoryId categoryId, SubcategoryId? subcategoryId = null)
    {
        EnsureNotDeleted();
        RaiseEvent(new ExpenseUpdated(
            Id, UserId.Value, null, null, null, null, categoryId.Value, subcategoryId?.Value, null, null,
            DateTimeOffset.UtcNow));
    }

    public void Update(
        decimal? amount = null, Currency? currency = null, DateOnly? date = null,
        string? description = null, CategoryId? categoryId = null, SubcategoryId? subcategoryId = null,
        bool? recurring = null, Dictionary<string, string>? metadata = null)
    {
        EnsureNotDeleted();
        if (amount.HasValue && amount.Value <= 0)
            throw new DomainException("Expense amount must be positive.");

        RaiseEvent(new ExpenseUpdated(
            Id, UserId.Value, amount, currency, date, description, categoryId?.Value, subcategoryId?.Value, recurring, metadata,
            DateTimeOffset.UtcNow));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        RaiseEvent(new ExpenseDeleted(Id, UserId.Value, DateTimeOffset.UtcNow));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify a deleted expense.");
    }

    #region Apply

    public void Apply(ExpenseRecorded e)
    {
        Id = e.ExpenseId;
        UserId = new UserId(e.UserId);
        AccountId = new AccountId(e.AccountId);
        CategoryId = new CategoryId(e.CategoryId);
        SubcategoryId = e.SubcategoryId is not null ? new SubcategoryId(e.SubcategoryId) : null;
        Amount = e.Amount;
        Currency = e.Currency;
        Date = e.Date;
        Description = e.Description;
        Recurring = e.Recurring;
        Metadata = e.Metadata;
        IsDeleted = false;
    }

    public void Apply(ExpenseUpdated e)
    {
        if (e.Amount.HasValue) Amount = e.Amount.Value;
        if (e.Currency.HasValue) Currency = e.Currency.Value;
        if (e.Date.HasValue) Date = e.Date.Value;
        if (e.Description is not null) Description = e.Description;
        if (e.CategoryId is not null) CategoryId = new CategoryId(e.CategoryId);
        if (e.SubcategoryId is not null) SubcategoryId = new SubcategoryId(e.SubcategoryId);
        if (e.Recurring.HasValue) Recurring = e.Recurring.Value;
        if (e.Metadata is not null) Metadata = e.Metadata;
    }

    public void Apply(ExpenseDeleted e)
    {
        IsDeleted = true;
    }

    #endregion
}

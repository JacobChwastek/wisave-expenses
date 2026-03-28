namespace WiSave.Expenses.Contracts.Models;

public sealed record UserId(string Value)
{
    public static explicit operator string(UserId id) => id.Value;
    public static explicit operator UserId(string value) => new(value);
}

public sealed record AccountId(string Value)
{
    public static explicit operator string(AccountId id) => id.Value;
    public static explicit operator AccountId(string value) => new(value);
}

public sealed record ExpenseId(string Value)
{
    public static explicit operator string(ExpenseId id) => id.Value;
    public static explicit operator ExpenseId(string value) => new(value);
}

public sealed record BudgetId(string Value)
{
    public static explicit operator string(BudgetId id) => id.Value;
    public static explicit operator BudgetId(string value) => new(value);
}

public sealed record CategoryId(string Value)
{
    public static explicit operator string(CategoryId id) => id.Value;
    public static explicit operator CategoryId(string value) => new(value);
}

public sealed record SubcategoryId(string Value)
{
    public static explicit operator string(SubcategoryId id) => id.Value;
    public static explicit operator SubcategoryId(string value) => new(value);
}

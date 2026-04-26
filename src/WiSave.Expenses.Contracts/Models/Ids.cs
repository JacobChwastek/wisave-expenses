namespace WiSave.Expenses.Contracts.Models;

public sealed record UserId(string Value)
{
    public static explicit operator string(UserId id) => id.Value;
    public static explicit operator UserId(string value) => new(value);
}

public sealed record FundingAccountId(string Value)
{
    public static explicit operator string(FundingAccountId id) => id.Value;
    public static explicit operator FundingAccountId(string value) => new(value);
}

public sealed record PaymentInstrumentId(string Value)
{
    public static explicit operator string(PaymentInstrumentId id) => id.Value;
    public static explicit operator PaymentInstrumentId(string value) => new(value);
}

public sealed record TransferId(string Value)
{
    public static explicit operator string(TransferId id) => id.Value;
    public static explicit operator TransferId(string value) => new(value);
}

public sealed record ExpenseId(string Value)
{
    public static explicit operator string(ExpenseId id) => id.Value;
    public static explicit operator ExpenseId(string value) => new(value);
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

namespace WiSave.Expenses.Projections.ReadModels;

public sealed class CreditCardAccountReadModel
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string SettlementAccountId { get; set; } = string.Empty;
    public string BankProvider { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int StatementClosingDay { get; set; }
    public int GracePeriodDays { get; set; }
    public decimal UnbilledBalance { get; set; }
    public decimal? ActiveStatementBalance { get; set; }
    public decimal? ActiveStatementOutstandingBalance { get; set; }
    public decimal? ActiveStatementMinimumPaymentDue { get; set; }
    public DateOnly? ActiveStatementDueDate { get; set; }
    public DateOnly? ActiveStatementPeriodCloseDate { get; set; }
    public string? Color { get; set; }
    public string? LastFourDigits { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

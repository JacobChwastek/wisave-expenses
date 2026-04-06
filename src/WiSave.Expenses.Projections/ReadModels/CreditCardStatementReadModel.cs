namespace WiSave.Expenses.Projections.ReadModels;

public sealed class CreditCardStatementReadModel
{
    public string Id { get; set; } = string.Empty;
    public string CreditCardAccountId { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly StatementDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal MinimumPaymentDue { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

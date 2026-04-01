namespace WiSave.Expenses.Projections.ReadModels;

public sealed class AccountReadModel
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? BillingCycleDay { get; set; }
    public decimal? CurrentDebt { get; set; }
    public string? LinkedBankAccountId { get; set; }
    public string? Color { get; set; }
    public string? LastFourDigits { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

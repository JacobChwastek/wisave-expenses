namespace WiSave.Expenses.Projections.ReadModels;

public sealed class FundingPaymentInstrumentReadModel
{
    public string Id { get; set; } = string.Empty;
    public string FundingAccountId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public string? Network { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

namespace WiSave.Expenses.Projections.ReadModels;

public sealed class BudgetReadModel
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalLimit { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool Recurring { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

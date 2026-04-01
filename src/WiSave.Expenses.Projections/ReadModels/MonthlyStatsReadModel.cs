namespace WiSave.Expenses.Projections.ReadModels;

public sealed class MonthlyStatsReadModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSpent { get; set; }
    public string Currency { get; set; } = string.Empty;
}

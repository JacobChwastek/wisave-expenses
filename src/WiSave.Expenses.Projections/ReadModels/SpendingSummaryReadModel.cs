namespace WiSave.Expenses.Projections.ReadModels;

public sealed class SpendingSummaryReadModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
}

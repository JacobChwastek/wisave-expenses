namespace WiSave.Expenses.Projections.ReadModels;

public sealed class BudgetCategoryLimitReadModel
{
    public int Id { get; set; }
    public string BudgetId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
}

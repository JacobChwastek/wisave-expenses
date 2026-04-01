namespace WiSave.Expenses.Core.Infrastructure.Postgres.Entities;

public sealed class SubcategoryEntity
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

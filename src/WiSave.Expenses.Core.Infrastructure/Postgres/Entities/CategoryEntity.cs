namespace WiSave.Expenses.Core.Infrastructure.Postgres.Entities;

public sealed class CategoryEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

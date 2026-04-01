namespace WiSave.Expenses.Projections.ReadModels;

public sealed class ProjectionCheckpoint
{
    public string Id { get; set; } = string.Empty;
    public ulong Position { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

namespace WiSave.Expenses.Projections.ReadModels;

public sealed class ProcessedMessageReadModel
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}

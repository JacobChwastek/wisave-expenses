namespace WiSave.Expenses.Contracts.Events;

public sealed record CommandFailed(
    Guid CorrelationId,
    string UserId,
    string CommandType,
    string Reason,
    DateTimeOffset Timestamp);

namespace WiSave.Expenses.Core.Application.Abstractions;

public sealed record CommandResult
{
    public bool IsSuccess { get; private init; }
    public string? EntityId { get; private init; }
    public string? Error { get; private init; }

    public static CommandResult Success(string entityId) => new() { IsSuccess = true, EntityId = entityId };
    public static CommandResult Failure(string error) => new() { IsSuccess = false, Error = error };
}

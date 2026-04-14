namespace WiSave.Expenses.Projections.Queries;

public sealed record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public string? PreviousCursor { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

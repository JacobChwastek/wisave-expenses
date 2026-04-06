namespace WiSave.Expenses.Projections.Queries;

public sealed record ExpenseQueryParams
{
    public string UserId { get; init; } = string.Empty;
    public string? Cursor { get; init; }
    public int PageSize { get; init; } = 20;
    public string Direction { get; init; } = "next";
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Search { get; init; }
    public List<string>? CategoryIds { get; init; }
    public List<string>? AccountIds { get; init; }
    public bool? Recurring { get; init; }
    public string SortField { get; init; } = "date";
    public string SortDirection { get; init; } = "desc";
}

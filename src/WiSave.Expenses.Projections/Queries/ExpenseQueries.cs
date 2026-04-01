using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

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

public sealed record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public string? PreviousCursor { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public sealed class ExpenseQueries(ProjectionsDbContext db)
{
    public async Task<PagedResult<ExpenseReadModel>> GetPagedAsync(ExpenseQueryParams p, CancellationToken ct = default)
    {
        var query = db.Expenses
            .Where(e => e.UserId == p.UserId && !e.IsDeleted)
            .AsNoTracking();

        // Filters
        if (p.From.HasValue) query = query.Where(e => e.Date >= p.From.Value);
        if (p.To.HasValue) query = query.Where(e => e.Date <= p.To.Value);
        if (!string.IsNullOrWhiteSpace(p.Search)) query = query.Where(e => EF.Functions.ILike(e.Description, $"%{p.Search}%"));
        if (p.CategoryIds is { Count: > 0 }) query = query.Where(e => p.CategoryIds.Contains(e.CategoryId));
        if (p.AccountIds is { Count: > 0 }) query = query.Where(e => p.AccountIds.Contains(e.AccountId));
        if (p.Recurring.HasValue) query = query.Where(e => e.Recurring == p.Recurring.Value);

        // Sort
        query = (p.SortField, p.SortDirection) switch
        {
            ("date", "asc") => query.OrderBy(e => e.Date).ThenBy(e => e.Id),
            ("date", _) => query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
            ("amount", "asc") => query.OrderBy(e => e.Amount).ThenBy(e => e.Id),
            ("amount", _) => query.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Id),
            ("description", "asc") => query.OrderBy(e => e.Description).ThenBy(e => e.Id),
            ("description", _) => query.OrderByDescending(e => e.Description).ThenByDescending(e => e.Id),
            _ => query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
        };

        // Cursor pagination
        var items = await query.Take(p.PageSize + 1).ToListAsync(ct);
        var hasMore = items.Count > p.PageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return new PagedResult<ExpenseReadModel>
        {
            Items = items,
            HasNextPage = hasMore,
            HasPreviousPage = p.Cursor is not null,
            NextCursor = hasMore && items.Count > 0 ? items[^1].Id : null,
            PreviousCursor = p.Cursor,
        };
    }

    public async Task<ExpenseReadModel?> GetByIdAsync(string expenseId, string userId, CancellationToken ct = default)
    {
        return await db.Expenses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.UserId == userId && !e.IsDeleted, ct);
    }
}

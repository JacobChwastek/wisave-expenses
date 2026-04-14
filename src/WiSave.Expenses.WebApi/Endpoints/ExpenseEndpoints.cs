using MassTransit;
using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Queries;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.Expenses;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses").WithTags("Expenses");

        group.MapPost("/", Record).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}", Update).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}", Delete).RequirePermission(Permissions.Expenses.Delete);
        group.MapGet("/", GetPaged).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}", GetById).RequirePermission(Permissions.Expenses.Read);
    }

    private static async Task<IResult> Record(
        IPublishEndpoint bus, ICurrentUser user, RecordExpenseRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new RecordExpense(
            correlationId, user.UserId, request.AccountId, request.CategoryId, request.SubcategoryId,
            request.Amount, request.Currency, request.Date, request.Description,
            request.Recurring, request.Metadata));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Update(
        string id, IPublishEndpoint bus, ICurrentUser user, UpdateExpenseRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new UpdateExpense(
            correlationId, user.UserId, id,
            request.Amount, request.Currency, request.Date, request.Description,
            request.CategoryId, request.SubcategoryId, request.Recurring, request.Metadata));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Delete(
        string id, IPublishEndpoint bus, ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new DeleteExpense(correlationId, user.UserId, id));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> GetPaged(
        ICurrentUser user, ExpenseReadRepository repository,
        string? cursor, int? pageSize, string? direction,
        string? from, string? to, string? search,
        string? categoryIds, string? accountIds,
        bool? recurring, string? sortField, string? sortDirection)
    {
        var result = await repository.GetPagedAsync(new ExpenseQueryParams
        {
            UserId = user.UserId,
            Cursor = cursor,
            PageSize = pageSize ?? 20,
            Direction = direction ?? "next",
            From = from is not null ? DateOnly.FromDateTime(DateTime.Parse(from)) : null,
            To = to is not null ? DateOnly.FromDateTime(DateTime.Parse(to)) : null,
            Search = search,
            CategoryIds = categoryIds?.Split(',').ToList(),
            AccountIds = accountIds?.Split(',').ToList(),
            Recurring = recurring,
            SortField = sortField ?? "date",
            SortDirection = sortDirection ?? "desc",
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(
        string id, ICurrentUser user, ExpenseReadRepository repository)
    {
        var expense = await repository.GetByIdAsync(id, user.UserId);
        return expense is not null ? Results.Ok(expense) : Results.NotFound();
    }
}

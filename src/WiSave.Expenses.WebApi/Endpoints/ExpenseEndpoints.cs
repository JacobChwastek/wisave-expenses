using MassTransit;
using WiSave.Expenses.Contracts.Commands.Expenses;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Queries;

namespace WiSave.Expenses.WebApi.Endpoints.Expenses;

public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses").WithTags("Expenses");

        // Commands
        group.MapPost("/", async (IPublishEndpoint bus, ICurrentUser user, RecordExpenseRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new RecordExpense(
                correlationId, user.UserId, request.AccountId, request.CategoryId, request.SubcategoryId,
                request.Amount, request.Currency, request.Date, request.Description,
                request.Recurring, request.Metadata));

            return Results.Accepted(value: new { correlationId });
        });

        group.MapPut("/{id}", async (string id, IPublishEndpoint bus, ICurrentUser user, UpdateExpenseRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new UpdateExpense(
                correlationId, user.UserId, id,
                request.Amount, request.Currency, request.Date, request.Description,
                request.CategoryId, request.SubcategoryId, request.Recurring, request.Metadata));

            return Results.Accepted(value: new { correlationId });
        });

        group.MapDelete("/{id}", async (string id, IPublishEndpoint bus, ICurrentUser user) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new DeleteExpense(correlationId, user.UserId, id));
            return Results.Accepted(value: new { correlationId });
        });

        // Queries
        group.MapGet("/", async (
            ICurrentUser user, ExpenseQueries queries,
            string? cursor, int? pageSize, string? direction,
            DateOnly? from, DateOnly? to, string? search,
            string? categoryIds, string? accountIds,
            bool? recurring, string? sortField, string? sortDirection) =>
        {
            var result = await queries.GetPagedAsync(new ExpenseQueryParams
            {
                UserId = user.UserId,
                Cursor = cursor,
                PageSize = pageSize ?? 20,
                Direction = direction ?? "next",
                From = from,
                To = to,
                Search = search,
                CategoryIds = categoryIds?.Split(',').ToList(),
                AccountIds = accountIds?.Split(',').ToList(),
                Recurring = recurring,
                SortField = sortField ?? "date",
                SortDirection = sortDirection ?? "desc",
            });

            return Results.Ok(result);
        });

        group.MapGet("/{id}", async (string id, ICurrentUser user, ExpenseQueries queries) =>
        {
            var expense = await queries.GetByIdAsync(id, user.UserId);
            return expense is not null ? Results.Ok(expense) : Results.NotFound();
        });
    }
}

public sealed record RecordExpenseRequest(
    string AccountId, string CategoryId, string? SubcategoryId,
    decimal Amount, Currency Currency, DateOnly Date, string Description,
    bool Recurring = false, Dictionary<string, string>? Metadata = null);

public sealed record UpdateExpenseRequest(
    decimal? Amount = null, Currency? Currency = null, DateOnly? Date = null,
    string? Description = null, string? CategoryId = null, string? SubcategoryId = null,
    bool? Recurring = null, Dictionary<string, string>? Metadata = null);

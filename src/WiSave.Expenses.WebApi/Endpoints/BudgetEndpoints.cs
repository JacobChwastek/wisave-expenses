using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Queries;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budgets").WithTags("Budgets");

        // Commands
        group.MapPost("/", async (IPublishEndpoint bus, ICurrentUser user, CreateBudgetRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new CreateBudget(
                correlationId, user.UserId, request.Month, request.Year,
                request.TotalLimit, request.Currency, request.Recurring));

            return Results.Accepted(value: new { correlationId });
        });

        group.MapPost("/copy", async (IPublishEndpoint bus, ICurrentUser user, CopyBudgetRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new CopyBudgetFromPrevious(correlationId, user.UserId, request.Month, request.Year));
            return Results.Accepted(value: new { correlationId });
        });

        group.MapPut("/{id}/limit", async (string id, IPublishEndpoint bus, ICurrentUser user, SetLimitRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new SetOverallLimit(correlationId, user.UserId, id, request.TotalLimit));
            return Results.Accepted(value: new { correlationId });
        });

        group.MapPut("/{id}/categories/{categoryId}", async (
            string id, string categoryId, IPublishEndpoint bus, ICurrentUser user, SetCategoryLimitRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new SetCategoryLimit(correlationId, user.UserId, id, categoryId, request.Limit));
            return Results.Accepted(value: new { correlationId });
        });

        group.MapDelete("/{id}/categories/{categoryId}", async (
            string id, string categoryId, IPublishEndpoint bus, ICurrentUser user) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new RemoveCategoryLimit(correlationId, user.UserId, id, categoryId));
            return Results.Accepted(value: new { correlationId });
        });

        // Queries
        group.MapGet("/", async (int month, int year, ICurrentUser user, BudgetQueries queries) =>
        {
            var budget = await queries.GetByMonthAsync(user.UserId, month, year);
            if (budget is null) return Results.NotFound();

            var categoryLimits = await queries.GetCategoryLimitsAsync(budget.Id);
            return Results.Ok(new { budget, categoryLimits });
        });

        group.MapGet("/summary", async (int month, int year, ICurrentUser user, BudgetQueries queries) =>
            Results.Ok(await queries.GetSpendingSummaryAsync(user.UserId, month, year)));

        group.MapGet("/monthly-stats", async (int year, ICurrentUser user, BudgetQueries queries) =>
            Results.Ok(await queries.GetMonthlyStatsAsync(user.UserId, year)));
    }
}

public sealed record CreateBudgetRequest(int Month, int Year, decimal TotalLimit, Currency Currency, bool Recurring = true);
public sealed record CopyBudgetRequest(int Month, int Year);
public sealed record SetLimitRequest(decimal TotalLimit);
public sealed record SetCategoryLimitRequest(decimal Limit);

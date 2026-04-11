using MassTransit;
using WiSave.Expenses.Contracts.Commands.Budgets;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.Budgets;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses/budgets").WithTags("Budgets");

        group.MapPost("/", Create).RequirePermission(Permissions.Expenses.Write);
        group.MapPost("/copy", Copy).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}/limit", SetLimit).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}/categories/{categoryId}", SetCategoryLimit).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}/categories/{categoryId}", RemoveCategoryLimit).RequirePermission(Permissions.Expenses.Delete);
        group.MapGet("/", GetByMonth).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/summary", GetSummary).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/monthly-stats", GetMonthlyStats).RequirePermission(Permissions.Expenses.Read);
    }

    private static async Task<IResult> Create(
        IPublishEndpoint bus, ICurrentUser user, CreateBudgetRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new CreateBudget(
            correlationId, user.UserId, request.Month, request.Year,
            request.TotalLimit, request.Currency, request.Recurring));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Copy(
        IPublishEndpoint bus, ICurrentUser user, CopyBudgetRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new CopyBudgetFromPrevious(correlationId, user.UserId, request.Month, request.Year));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> SetLimit(
        string id, IPublishEndpoint bus, ICurrentUser user, SetLimitRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new SetOverallLimit(correlationId, user.UserId, id, request.TotalLimit));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> SetCategoryLimit(
        string id, string categoryId, IPublishEndpoint bus, ICurrentUser user, SetCategoryLimitRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new SetCategoryLimit(correlationId, user.UserId, id, categoryId, request.Limit));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> RemoveCategoryLimit(
        string id, string categoryId, IPublishEndpoint bus, ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new Contracts.Commands.Budgets.RemoveCategoryLimit(correlationId, user.UserId, id, categoryId));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> GetByMonth(
        int month, int year, ICurrentUser user, BudgetReadRepository repository)
    {
        var budget = await repository.GetByMonthAsync(user.UserId, month, year);
        if (budget is null) return Results.NotFound();

        var categoryLimits = await repository.GetCategoryLimitsAsync(budget.Id);
        return Results.Ok(new { budget, categoryLimits });
    }

    private static async Task<IResult> GetSummary(
        int month, int year, ICurrentUser user, BudgetReadRepository repository)
    {
        return Results.Ok(await repository.GetSpendingSummaryAsync(user.UserId, month, year));
    }

    private static async Task<IResult> GetMonthlyStats(
        int year, ICurrentUser user, BudgetReadRepository repository)
    {
        return Results.Ok(await repository.GetMonthlyStatsAsync(user.UserId, year));
    }
}

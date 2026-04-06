using MassTransit;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.CreditCards;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class CreditCardAccountEndpoints
{
    public static void MapCreditCardAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses/credit-cards").WithTags("Credit Cards");

        group.MapPost("/", Open).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}", Update).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}", Close).RequirePermission(Permissions.Expenses.Delete);
        group.MapPost("/{id}/seed-state", SeedState).RequirePermission(Permissions.Expenses.Write);
        group.MapPost("/{id}/issue-statement", IssueStatement).RequirePermission(Permissions.Expenses.Write);
        group.MapGet("/", GetAll).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}", GetById).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}/statements", GetStatements).RequirePermission(Permissions.Expenses.Read);
    }

    private static async Task<IResult> Open(IPublishEndpoint bus, ICurrentUser user, OpenCreditCardAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        var command = request.ToCommand(correlationId, Guid.Parse(user.UserId));

        await bus.Publish(command);

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Update(string id, IPublishEndpoint bus, ICurrentUser user, UpdateCreditCardAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Close(string id, IPublishEndpoint bus, ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new CloseCreditCardAccount(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> SeedState(string id, IPublishEndpoint bus, ICurrentUser user, SeedCreditCardStateRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> IssueStatement(string id, IPublishEndpoint bus, ICurrentUser user, IssueCreditCardStatementRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> GetAll(ICurrentUser user, CreditCardAccountReadRepository repository)
    {
        return Results.Ok(await repository.GetAllAsync(user.UserId));
    }

    private static async Task<IResult> GetById(string id, ICurrentUser user, CreditCardAccountReadRepository repository)
    {
        var account = await repository.GetByIdAsync(id, user.UserId);
        return account is not null ? Results.Ok(account) : Results.NotFound();
    }

    private static async Task<IResult> GetStatements(string id, ICurrentUser user, CreditCardAccountReadRepository repository)
    {
        var account = await repository.GetByIdAsync(id, user.UserId);
        if (account is null)
            return Results.NotFound();

        return Results.Ok(await repository.GetStatementsAsync(id, user.UserId));
    }
}

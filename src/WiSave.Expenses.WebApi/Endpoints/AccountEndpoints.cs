using MassTransit;
using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.Accounts;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses/accounts").WithTags("Accounts");

        group.MapPost("/", Open).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}", Update).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}", Close).RequirePermission(Permissions.Expenses.Delete);
        group.MapGet("/", GetAll).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}", GetById).RequirePermission(Permissions.Expenses.Read);
    }

    private static async Task<IResult> Open(IPublishEndpoint bus, ICurrentUser user, OpenAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new OpenAccount(
            correlationId, user.UserId, request.Name, request.Type, request.Currency, request.Balance,
            request.LinkedBankAccountId, request.CreditLimit, request.BillingCycleDay,
            request.Color, request.LastFourDigits));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Update(string id, IPublishEndpoint bus, ICurrentUser user, UpdateAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new UpdateAccount(
            correlationId, user.UserId, id, request.Name, request.Type, request.Currency, request.Balance,
            request.LinkedBankAccountId, request.CreditLimit, request.BillingCycleDay,
            request.Color, request.LastFourDigits));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Close(string id, IPublishEndpoint bus, ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new CloseAccount(correlationId, user.UserId, id));
        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> GetAll(ICurrentUser user, AccountReadRepository repository)
    {
        return Results.Ok(await repository.GetAllAsync(user.UserId));
    }

    private static async Task<IResult> GetById(string id, ICurrentUser user, AccountReadRepository repository)
    {
        var account = await repository.GetByIdAsync(id, user.UserId);
        return account is not null ? Results.Ok(account) : Results.NotFound();
    }
}

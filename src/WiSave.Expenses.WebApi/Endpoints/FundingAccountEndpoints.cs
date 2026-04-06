using MassTransit;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.FundingAccounts;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class FundingAccountEndpoints
{
    public static void MapFundingAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses/funding-accounts").WithTags("Funding Accounts");

        group.MapPost("/", Open).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}", Update).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}", Close).RequirePermission(Permissions.Expenses.Delete);
        group.MapPost("/{id}/transfers", PostTransfer).RequirePermission(Permissions.Expenses.Write);
        group.MapPost("/{id}/payment-instruments", AddPaymentInstrument).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}/payment-instruments/{paymentInstrumentId}", UpdatePaymentInstrument).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}/payment-instruments/{paymentInstrumentId}", RemovePaymentInstrument).RequirePermission(Permissions.Expenses.Delete);
        group.MapGet("/", GetAll).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}", GetById).RequirePermission(Permissions.Expenses.Read);
        group.MapGet("/{id}/payment-instruments", GetPaymentInstruments).RequirePermission(Permissions.Expenses.Read);
    }

    private static async Task<IResult> Open(IPublishEndpoint bus, ICurrentUser user, OpenFundingAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        var command = request.ToCommand(correlationId, Guid.Parse(user.UserId));

        await bus.Publish(command);

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Update(string id, IPublishEndpoint bus, ICurrentUser user, UpdateFundingAccountRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> Close(string id, IPublishEndpoint bus, ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new CloseFundingAccount(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> AddPaymentInstrument(
        string id,
        IPublishEndpoint bus,
        ICurrentUser user,
        AddFundingPaymentInstrumentRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> UpdatePaymentInstrument(
        string id,
        string paymentInstrumentId,
        IPublishEndpoint bus,
        ICurrentUser user,
        UpdateFundingPaymentInstrumentRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id, paymentInstrumentId));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> RemovePaymentInstrument(
        string id,
        string paymentInstrumentId,
        IPublishEndpoint bus,
        ICurrentUser user)
    {
        var correlationId = Guid.CreateVersion7();
        await bus.Publish(new RemoveFundingPaymentInstrument(correlationId, user.UserId, id, paymentInstrumentId));

        return Results.Accepted(value: new { correlationId });
    }

    private static async Task<IResult> PostTransfer(
        string id,
        IPublishEndpoint bus,
        ICurrentUser user,
        PostFundingTransferRequest request)
    {
        var correlationId = Guid.CreateVersion7();
        var transferId = Guid.CreateVersion7().ToString();
        await bus.Publish(request.ToCommand(correlationId, user.UserId, id, transferId));

        return Results.Accepted(value: new { correlationId, transferId });
    }

    private static async Task<IResult> GetAll(ICurrentUser user, FundingAccountReadRepository repository)
    {
        return Results.Ok(await repository.GetAllAsync(user.UserId));
    }

    private static async Task<IResult> GetById(string id, ICurrentUser user, FundingAccountReadRepository repository)
    {
        var account = await repository.GetByIdAsync(id, user.UserId);
        return account is not null ? Results.Ok(account) : Results.NotFound();
    }

    private static async Task<IResult> GetPaymentInstruments(
        string id,
        ICurrentUser user,
        FundingAccountReadRepository repository)
    {
        return Results.Ok(await repository.GetPaymentInstrumentsAsync(id, user.UserId));
    }
}

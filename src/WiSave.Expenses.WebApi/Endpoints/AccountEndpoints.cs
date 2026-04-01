using MassTransit;
using WiSave.Expenses.Contracts.Commands.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Projections.Queries;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithTags("Accounts");

        // Commands
        group.MapPost("/", async (IPublishEndpoint bus, ICurrentUser user, OpenAccountRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new OpenAccount(
                correlationId, user.UserId, request.Name, request.Type, request.Currency, request.Balance,
                request.LinkedBankAccountId, request.CreditLimit, request.BillingCycleDay,
                request.Color, request.LastFourDigits));

            return Results.Accepted(value: new { correlationId });
        });

        group.MapPut("/{id}", async (string id, IPublishEndpoint bus, ICurrentUser user, UpdateAccountRequest request) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new UpdateAccount(
                correlationId, user.UserId, id, request.Name, request.Type, request.Currency, request.Balance,
                request.LinkedBankAccountId, request.CreditLimit, request.BillingCycleDay,
                request.Color, request.LastFourDigits));

            return Results.Accepted(value: new { correlationId });
        });

        group.MapDelete("/{id}", async (string id, IPublishEndpoint bus, ICurrentUser user) =>
        {
            var correlationId = Guid.NewGuid();
            await bus.Publish(new CloseAccount(correlationId, user.UserId, id));
            return Results.Accepted(value: new { correlationId });
        });

        // Queries
        group.MapGet("/", async (ICurrentUser user, AccountQueries queries) =>
            Results.Ok(await queries.GetAllAsync(user.UserId)));

        group.MapGet("/{id}", async (string id, ICurrentUser user, AccountQueries queries) =>
        {
            var account = await queries.GetByIdAsync(id, user.UserId);
            return account is not null ? Results.Ok(account) : Results.NotFound();
        });
    }
}

public sealed record OpenAccountRequest(
    string Name, AccountType Type, Currency Currency, decimal Balance,
    string? LinkedBankAccountId = null, decimal? CreditLimit = null, int? BillingCycleDay = null,
    string? Color = null, string? LastFourDigits = null);

public sealed record UpdateAccountRequest(
    string Name, AccountType Type, Currency Currency, decimal Balance,
    string? LinkedBankAccountId = null, decimal? CreditLimit = null, int? BillingCycleDay = null,
    string? Color = null, string? LastFourDigits = null);

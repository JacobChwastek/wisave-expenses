using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.WebApi.Authorization;

namespace WiSave.Expenses.WebApi.Tests.Authorization;

public class PermissionEndpointFilterTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<PermissionContext>();

        _app = builder.Build();
        _app.MapGet("/test", () => Results.Ok("allowed"))
            .RequirePermission(TestPermissions.Read);

        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Request_WithCorrectPermission_Returns200()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("X-User-Id", "user-1");
        request.Headers.Add("X-User-Permissions", TestPermissions.Read);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutPermission_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("X-User-Id", "user-1");
        request.Headers.Add("X-User-Permissions", "other:read");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWildcard_Returns200()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("X-User-Id", "user-1");
        request.Headers.Add("X-User-Permissions", "*");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutUserIdHeader_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("X-User-Permissions", TestPermissions.Read);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutPermissionsHeader_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("X-User-Id", "user-1");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

using Microsoft.AspNetCore.Http;
using WiSave.Expenses.WebApi.Authorization;

namespace WiSave.Expenses.WebApi.Tests.Authorization;

public class PermissionContextTests
{
    [Fact]
    public void Permissions_ParsesCommaDelimitedHeader()
    {
        var context = CreateContext(permissions: $"{Permissions.Expenses.Read}, {Permissions.Expenses.Write}");
        Assert.Contains(Permissions.Expenses.Read, context.Permissions);
        Assert.Contains(Permissions.Expenses.Write, context.Permissions);
    }

    [Fact]
    public void Permissions_MissingHeader_ReturnsEmptySet()
    {
        var context = CreateContext();
        Assert.Empty(context.Permissions);
    }

    [Fact]
    public void Permissions_EmptyHeader_ReturnsEmptySet()
    {
        var context = CreateContext(permissions: "");
        Assert.Empty(context.Permissions);
    }

    [Fact]
    public void HasUserId_ReturnsTrueWhenPresent()
    {
        var context = CreateContext(userId: "user-123");
        Assert.True(context.HasUserId);
    }

    [Fact]
    public void HasUserId_ReturnsFalseWhenMissing()
    {
        var context = CreateContext();
        Assert.False(context.HasUserId);
    }

    [Fact]
    public void HasPermission_ExactMatch_ReturnsTrue()
    {
        var context = CreateContext(permissions: $"{Permissions.Expenses.Read},{Permissions.Expenses.Write}");
        Assert.True(context.HasPermission(Permissions.Expenses.Read));
        Assert.True(context.HasPermission(Permissions.Expenses.Write));
        Assert.False(context.HasPermission(Permissions.Expenses.Delete));
    }

    [Fact]
    public void HasPermission_Wildcard_ReturnsTrue()
    {
        var context = CreateContext(permissions: "*");
        Assert.True(context.HasPermission("anything"));
    }

    [Fact]
    public void HasPermission_CaseInsensitive()
    {
        var context = CreateContext(permissions: "Expenses:Read");
        Assert.True(context.HasPermission(Permissions.Expenses.Read));
    }

    private static PermissionContext CreateContext(string? userId = null, string? permissions = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
            httpContext.Request.Headers["X-User-Id"] = userId;
        if (permissions is not null)
            httpContext.Request.Headers["X-User-Permissions"] = permissions;

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new PermissionContext(accessor);
    }
}

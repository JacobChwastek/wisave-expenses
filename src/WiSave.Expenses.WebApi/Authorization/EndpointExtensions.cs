namespace WiSave.Expenses.WebApi.Authorization;

public static class EndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder
            .WithMetadata(new PermissionMetadata(permission))
            .AddEndpointFilter<PermissionEndpointFilter>();
    }
}

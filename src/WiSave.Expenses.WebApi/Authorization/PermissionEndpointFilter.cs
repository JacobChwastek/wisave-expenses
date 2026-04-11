namespace WiSave.Expenses.WebApi.Authorization;

public sealed class PermissionEndpointFilter(PermissionContext permissionContext) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<PermissionMetadata>();
        if (metadata is null)
            return await next(context);

        if (!permissionContext.HasUserId)
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        if (!permissionContext.HasPermission(metadata.RequiredPermission))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        return await next(context);
    }
}

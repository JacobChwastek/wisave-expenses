namespace WiSave.Expenses.WebApi.Authorization;

public sealed class PermissionContext(IHttpContextAccessor httpContextAccessor)
{
    private HashSet<string>? _permissions;

    public IReadOnlySet<string> Permissions => _permissions ??= ParsePermissions();

    public bool HasUserId =>
        !string.IsNullOrEmpty(httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault());

    public bool HasPermission(string permission) =>
        Permissions.Contains("*") || Permissions.Contains(permission);

    private HashSet<string> ParsePermissions()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers["X-User-Permissions"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
            return [];

        return header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

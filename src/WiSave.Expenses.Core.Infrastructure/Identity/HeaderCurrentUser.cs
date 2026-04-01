using Microsoft.AspNetCore.Http;

namespace WiSave.Expenses.Core.Infrastructure.Identity;

public sealed class HeaderCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId => httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault()
        ?? throw new InvalidOperationException("X-User-Id header is missing.");

    public string Email => httpContextAccessor.HttpContext?.Request.Headers["X-User-Email"].FirstOrDefault()
        ?? string.Empty;
}

using Microsoft.Extensions.DependencyInjection;

namespace WiSave.Expenses.Core.Infrastructure.Identity;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HeaderCurrentUser>();

        return services;
    }
}

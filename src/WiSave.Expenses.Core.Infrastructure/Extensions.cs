using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Core.Infrastructure.EventStore;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Core.Infrastructure.Postgres;

namespace WiSave.Expenses.Core.Infrastructure;

public static class Extensions
{
    public static IServiceCollection AddExpensesInfrastructure(
        this IServiceCollection services,
        string eventStoreConnectionString,
        string postgresConnectionString)
    {
        services.AddEventStore(eventStoreConnectionString);
        services.AddPostgres(postgresConnectionString);
        services.AddIdentity();

        return services;
    }
}

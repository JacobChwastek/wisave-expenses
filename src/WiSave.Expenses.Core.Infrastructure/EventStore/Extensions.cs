using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Framework.Application;
using WiSave.Framework.EventSourcing;
using WiSave.Framework.EventSourcing.Kurrent;

namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public static class EventStoreExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        WiSave.Framework.EventSourcing.Kurrent.Extensions.AddKurrentEventStore(services, connectionString);
        services.AddSingleton<IEventTypeRegistry>(_ => AssemblyEventTypeRegistry.FromAssemblies(
            [typeof(Contracts.Events.CommandFailed).Assembly],
            type => type.Namespace?.Contains(".Events.", StringComparison.Ordinal) == true));
        services.AddScoped<IAggregateRepository<FundingAccount, FundingAccountId>, KurrentDbAggregateRepository<FundingAccount, FundingAccountId>>();
        services.AddScoped<IFundingAccountLookup, FundingAccountLookup>();

        return services;
    }

    public static IServiceCollection AddKurrentForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        WiSave.Framework.EventSourcing.Kurrent.Extensions.AddKurrentForwarding(services, configuration);

        return services;
    }
}

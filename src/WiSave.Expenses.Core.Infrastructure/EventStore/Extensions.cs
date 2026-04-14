using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Configuration;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Hosting;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public static class EventStoreExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);
        services.AddSingleton<ContractEventTypeRegistry>();
        services.AddSingleton(new EventStoreClient(settings));
        services.AddSingleton(new EventStorePersistentSubscriptionsClient(settings));
        services.AddScoped<IAggregateRepository<Account>, KurrentDbAggregateRepository<Account>>();
        services.AddScoped<IAggregateRepository<Expense>, KurrentDbAggregateRepository<Expense>>();
        services.AddScoped<IAggregateRepository<Budget>, KurrentDbAggregateRepository<Budget>>();

        return services;
    }

    public static IServiceCollection AddKurrentForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KurrentForwarderOptions>(configuration.GetSection("KurrentForwarder"));
        services.AddSingleton<IKurrentPersistentSubscriptionClient, EventStorePersistentSubscriptionClientAdapter>();
        services.AddSingleton<KurrentSubscriptionBootstrapper>();
        services.AddHostedService<KurrentToRabbitForwarder>();

        return services;
    }
}

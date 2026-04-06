using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.Budgeting;

namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public static class EventStoreExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);
        services.AddSingleton(new EventStoreClient(settings));
        services.AddScoped<IAggregateRepository<Account>, KurrentDbAggregateRepository<Account>>();
        services.AddScoped<IAggregateRepository<Expense>, KurrentDbAggregateRepository<Expense>>();
        services.AddScoped<IAggregateRepository<Budget>, KurrentDbAggregateRepository<Budget>>();

        return services;
    }
}

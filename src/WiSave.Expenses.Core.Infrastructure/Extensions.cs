using EventStore.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Accounting;
using WiSave.Expenses.Core.Domain.Budgeting;
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
        // KurrentDB
        var settings = EventStoreClientSettings.Create(eventStoreConnectionString);
        services.AddSingleton(new EventStoreClient(settings));
        services.AddScoped<IAggregateRepository<Account>, KurrentDbAggregateRepository<Account>>();
        services.AddScoped<IAggregateRepository<Expense>, KurrentDbAggregateRepository<Expense>>();
        services.AddScoped<IAggregateRepository<Budget>, KurrentDbAggregateRepository<Budget>>();

        // Postgres
        services.AddDbContext<ExpensesDbContext>(opts => opts.UseNpgsql(postgresConnectionString));
        services.AddScoped<ICategoryRepository, PostgresCategoryRepository>();

        // Identity
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HeaderCurrentUser>();

        return services;
    }
}

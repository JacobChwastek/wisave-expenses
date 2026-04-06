using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Domain.Budgeting;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.Funding;
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
        services.AddScoped<IAggregateRepository<Budget, BudgetId>, KurrentDbAggregateRepository<Budget, BudgetId>>();
        services.AddScoped<IAggregateRepository<FundingAccount, FundingAccountId>, KurrentDbAggregateRepository<FundingAccount, FundingAccountId>>();
        services.AddScoped<IAggregateRepository<CreditCardAccount, CreditCardAccountId>, KurrentDbAggregateRepository<CreditCardAccount, CreditCardAccountId>>();
        services.AddScoped<IFundingAccountLookup, FundingAccountLookup>();
        services.AddSingleton<ICreditCardStatementPolicyResolver, StatementPolicyResolver>();
        services.AddSingleton<ICreditCardPaymentAllocationPolicy, OldestStatementFirstCreditCardPaymentAllocationPolicy>();

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

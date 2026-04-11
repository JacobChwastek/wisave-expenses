using MassTransit;
using WiSave.Expenses.Core.Application.Accounting.Handlers;
using WiSave.Expenses.Core.Infrastructure;
using WiSave.Expenses.Core.Infrastructure.Messaging;
using WiSave.Expenses.Worker.Domain.Forwarding;

var builder = Host.CreateApplicationBuilder(args);

var eventStoreCs = builder.Configuration.GetConnectionString("EventStore")!;

builder.Services.AddExpensesInfrastructure(eventStoreCs, builder.Configuration.GetConnectionString("Postgres")!);
builder.Services.Configure<KurrentForwarderOptions>(builder.Configuration.GetSection("KurrentForwarder"));
builder.Services.AddSingleton<IKurrentPersistentSubscriptionClient, EventStorePersistentSubscriptionClientAdapter>();
builder.Services.AddSingleton<KurrentSubscriptionBootstrapper>();
builder.Services.AddHostedService<KurrentToRabbitForwarder>();

builder.Services.AddMessaging(builder.Configuration, x =>
{
    x.AddConsumers(typeof(OpenAccountHandler).Assembly);
});

var host = builder.Build();
host.Run();

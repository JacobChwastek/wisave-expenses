using MassTransit;
using WiSave.Expenses.Core.Application.Funding.Handlers;
using WiSave.Expenses.Core.Infrastructure;
using WiSave.Expenses.Core.Infrastructure.EventStore;
using WiSave.Expenses.Core.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

var eventStoreCs = builder.Configuration.GetConnectionString("EventStore")!;

builder.Services.AddExpensesInfrastructure(eventStoreCs, builder.Configuration.GetConnectionString("Postgres")!);
builder.Services.AddKurrentForwarding(builder.Configuration);

builder.Services.AddMessaging(builder.Configuration, x =>
{
    x.AddConsumers(typeof(OpenFundingAccountHandler).Assembly);
});

var host = builder.Build();
host.Run();

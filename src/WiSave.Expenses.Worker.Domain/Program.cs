using MassTransit;
using WiSave.Expenses.Core.Application.Accounting.Handlers;
using WiSave.Expenses.Core.Infrastructure;
using WiSave.Expenses.Core.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

var eventStoreCs = builder.Configuration.GetConnectionString("EventStore")!;
var postgresCs = builder.Configuration.GetConnectionString("Postgres")!;

builder.Services.AddExpensesInfrastructure(eventStoreCs, postgresCs);

builder.Services.AddMessaging(builder.Configuration, x =>
{
    x.AddConsumers(typeof(OpenAccountHandler).Assembly);
});

var host = builder.Build();
host.Run();

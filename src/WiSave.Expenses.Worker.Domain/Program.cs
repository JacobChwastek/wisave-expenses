using MassTransit;
using WiSave.Expenses.Core.Application.Accounting.Handlers;
using WiSave.Expenses.Core.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

var eventStoreCs = builder.Configuration.GetConnectionString("EventStore")
    ?? "esdb://localhost:2113?tls=false";
var postgresCs = builder.Configuration.GetConnectionString("Config")
    ?? "Host=localhost;Port=5433;Database=wisave_expenses;Username=wisave;Password=wisave_dev";

builder.Services.AddExpensesInfrastructure(eventStoreCs, postgresCs);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(OpenAccountHandler).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "expenses";

        cfg.Host(host, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();

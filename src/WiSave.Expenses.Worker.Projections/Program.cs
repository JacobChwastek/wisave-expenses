using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections;
using WiSave.Expenses.Projections.EventHandlers;

var builder = Host.CreateApplicationBuilder(args);

var postgresCs = builder.Configuration.GetConnectionString("Projections") ?? "Host=localhost;Port=5433;Database=wisave_expenses;Username=wisave;Password=wisave_dev";

builder.Services.AddDbContext<ProjectionsDbContext>(opts => opts.UseNpgsql(postgresCs));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(AccountEventHandler).Assembly);

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

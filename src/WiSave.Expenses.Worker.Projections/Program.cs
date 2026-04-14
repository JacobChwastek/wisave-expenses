using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections;
using WiSave.Expenses.Projections.EventHandlers;

var builder = Host.CreateApplicationBuilder(args);

var postgresCs = builder.Configuration.GetConnectionString("Postgres")!;

builder.Services.AddDbContext<ProjectionsDbContext>(opts => opts.UseNpgsql(postgresCs));

var rabbitMq = builder.Configuration.GetSection("RabbitMq");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(AccountEventHandler).Assembly);
    x.SetEndpointNameFormatter(new DefaultEndpointNameFormatter(".", null, true));
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMq["Host"], rabbitMq["VirtualHost"], h =>
        {
            h.Username(rabbitMq["Username"]!);
            h.Password(rabbitMq["Password"]!);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();

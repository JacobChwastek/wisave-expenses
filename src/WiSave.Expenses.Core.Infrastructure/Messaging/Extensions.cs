using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WiSave.Expenses.Core.Infrastructure.Messaging;

public static class Extensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitMq = configuration.GetSection("RabbitMq");

        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

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

        return services;
    }
}

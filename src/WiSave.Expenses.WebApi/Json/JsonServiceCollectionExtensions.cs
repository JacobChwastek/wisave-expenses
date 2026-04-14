using System.Text.Json.Serialization;

namespace WiSave.Expenses.WebApi.Json;

public static class JsonServiceCollectionExtensions
{
    public static IServiceCollection AddExpensesJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}

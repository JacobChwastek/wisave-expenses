using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WiSave.Expenses.Console.Infrastructure;
using WiSave.Expenses.Console.Shell;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationManager();
configuration.SetBasePath(AppContext.BaseDirectory);
configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
configuration.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false);
configuration.AddEnvironmentVariables();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(logging =>
{
    logging.AddSimpleConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});
services.AddExpensesConsole();

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});

var application = serviceProvider.GetRequiredService<IConsoleApplication>();
return await application.RunAsync(args, CancellationToken.None);

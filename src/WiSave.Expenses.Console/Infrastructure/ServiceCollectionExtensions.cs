using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExpensesConsole(this IServiceCollection services)
    {
        services.AddSingleton<IConsoleOutput, SystemConsoleOutput>();
        services.AddSingleton<ICommandCatalog, CommandCatalog>();
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<ICommandPrompter, CommandPrompter>();
        services.AddSingleton<ICommandRunner, CommandRunner>();
        services.AddSingleton<IConsoleShell, ConsoleShell>();
        services.AddSingleton<IConsoleApplication, ConsoleApplication>();

        services.AddSingleton<IScopedDatabaseMigrator, CoreDatabaseMigrator>();
        services.AddSingleton<IScopedDatabaseMigrator, ProjectionsDatabaseMigrator>();
        services.AddSingleton<IDatabaseMigrationOperations, DatabaseMigrationOperations>();

        RegisterCommands(services, typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }

    private static void RegisterCommands(IServiceCollection services, Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IExpensesCommand).IsAssignableFrom(type))
            .ToArray();

        foreach (var commandType in commandTypes)
        {
            services.AddTransient(typeof(IExpensesCommand), commandType);
        }
    }
}

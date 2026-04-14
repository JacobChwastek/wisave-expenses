using Microsoft.Extensions.Configuration;
using System.Text;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Operations;

internal interface IDatabaseMigrationOperations
{
    Task<string> RunAsync(string? connectionString, CancellationToken ct);
}

internal sealed class DatabaseMigrationOperations(
    IConfiguration configuration,
    IConsoleOutput consoleOutput,
    IEnumerable<IScopedDatabaseMigrator> migrators) : IDatabaseMigrationOperations
{
    public async Task<string> RunAsync(string? connectionString, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var effectiveConnectionString = NormalizeConnectionString(connectionString);
        if (string.IsNullOrWhiteSpace(effectiveConnectionString))
        {
            effectiveConnectionString = NormalizeConnectionString(configuration.GetConnectionString("Postgres"));
        }

        if (string.IsNullOrWhiteSpace(effectiveConnectionString))
        {
            throw new InvalidOperationException(
                "Postgres connection string was not configured. Set ConnectionStrings__Postgres or appsettings.json.");
        }

        var results = new List<MigrationRunResult>();
        foreach (var migrator in migrators)
        {
            consoleOutput.WriteLine($"Applying {migrator.Scope} migrations...");
            results.Add(await migrator.RunAsync(effectiveConnectionString, ct));
        }

        return FormatSuccessMessage(results);
    }

    private static string? NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var trimmed = connectionString.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '"' && trimmed[^1] == '"') ||
             (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static string FormatSuccessMessage(IReadOnlyList<MigrationRunResult> results)
    {
        var builder = new StringBuilder("Expenses database migrations applied.");

        foreach (var result in results)
        {
            builder.AppendLine();
            builder.Append(result.Scope);
            builder.Append(':');

            if (result.AppliedMigrations.Count == 0)
            {
                builder.AppendLine();
                builder.Append("- no new migrations");
                continue;
            }

            foreach (var migration in result.AppliedMigrations)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(FormatMigrationName(migration));
            }
        }

        return builder.ToString();
    }

    private static string FormatMigrationName(string migrationName)
        => migrationName.Split(".Scripts.", StringSplitOptions.None) switch
        {
            [_, var formattedName] => formattedName,
            _ => migrationName
        };
}

using Microsoft.Extensions.Configuration;
using CoreMigrator = WiSave.Expenses.Core.Migrations.DbMigrator;
using ProjectionsMigrator = WiSave.Expenses.Projections.Migrations.DbMigrator;

namespace WiSave.Expenses.Console.Operations;

internal interface IDatabaseMigrationOperations
{
    Task<string> RunAsync(string? connectionString, CancellationToken ct);
}

internal sealed class DatabaseMigrationOperations(IConfiguration configuration) : IDatabaseMigrationOperations
{
    public Task<string> RunAsync(string? connectionString, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var effectiveConnectionString = connectionString;
        if (string.IsNullOrWhiteSpace(effectiveConnectionString))
        {
            effectiveConnectionString = configuration.GetConnectionString("Postgres");
        }

        if (string.IsNullOrWhiteSpace(effectiveConnectionString))
        {
            throw new InvalidOperationException(
                "Postgres connection string was not configured. Set ConnectionStrings__Postgres or appsettings.json.");
        }

        CoreMigrator.Run(effectiveConnectionString);
        ProjectionsMigrator.Run(effectiveConnectionString);

        return Task.FromResult("Expenses database migrations applied.");
    }
}

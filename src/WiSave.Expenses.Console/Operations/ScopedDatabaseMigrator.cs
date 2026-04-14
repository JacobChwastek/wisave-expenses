using DbUp.Engine;
using CoreMigrator = WiSave.Expenses.Core.Migrations.DbMigrator;
using ProjectionsMigrator = WiSave.Expenses.Projections.Migrations.DbMigrator;

namespace WiSave.Expenses.Console.Operations;

internal sealed record MigrationRunResult(string Scope, IReadOnlyList<string> AppliedMigrations);

internal interface IScopedDatabaseMigrator
{
    string Scope { get; }

    Task<MigrationRunResult> RunAsync(string connectionString, CancellationToken ct);
}

internal sealed class CoreDatabaseMigrator : IScopedDatabaseMigrator
{
    public string Scope => "Core";

    public Task<MigrationRunResult> RunAsync(string connectionString, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = CoreMigrator.Run(connectionString);
        return Task.FromResult(ToRunResult(Scope, result));
    }

    private static MigrationRunResult ToRunResult(string scope, DatabaseUpgradeResult result)
        => new(scope, result.Scripts.Select(script => FormatMigrationName(script.Name)).ToArray());

    private static string FormatMigrationName(string scriptName)
        => scriptName.Split(".Scripts.", StringSplitOptions.None) switch
        {
            [_, var migrationName] => migrationName,
            _ => scriptName
        };
}

internal sealed class ProjectionsDatabaseMigrator : IScopedDatabaseMigrator
{
    public string Scope => "Projections";

    public Task<MigrationRunResult> RunAsync(string connectionString, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = ProjectionsMigrator.Run(connectionString);
        return Task.FromResult(ToRunResult(Scope, result));
    }

    private static MigrationRunResult ToRunResult(string scope, DatabaseUpgradeResult result)
        => new(scope, result.Scripts.Select(script => FormatMigrationName(script.Name)).ToArray());

    private static string FormatMigrationName(string scriptName)
        => scriptName.Split(".Scripts.", StringSplitOptions.None) switch
        {
            [_, var migrationName] => migrationName,
            _ => scriptName
        };
}

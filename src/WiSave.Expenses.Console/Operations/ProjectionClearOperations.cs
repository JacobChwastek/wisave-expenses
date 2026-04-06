using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WiSave.Expenses.Console.Operations;

internal sealed record ProjectionClearResult(string SchemaName, IReadOnlyList<string> ClearedTables)
{
    public string Format()
        => ClearedTables.Count == 0
            ? $"No projection tables were cleared in schema '{SchemaName}'."
            : $"Cleared {ClearedTables.Count} projection table(s) in schema '{SchemaName}'.";
}

internal interface IProjectionClearOperations
{
    Task<ProjectionClearResult> RunAsync(string? connectionString, CancellationToken ct);
}

internal interface IProjectionStorageResetClient
{
    Task<IReadOnlyList<string>> ListBaseTablesAsync(string connectionString, string schemaName, CancellationToken ct);
    Task TruncateTablesAsync(string connectionString, string schemaName, IReadOnlyList<string> tableNames, CancellationToken ct);
}

internal sealed class ProjectionClearOperations(
    IConfiguration configuration,
    IProjectionStorageResetClient client) : IProjectionClearOperations
{
    private const string SchemaName = "projections";
    private const string MigrationJournalTableName = "SchemaVersions";

    public async Task<ProjectionClearResult> RunAsync(string? connectionString, CancellationToken ct)
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

        var tableNames = await client.ListBaseTablesAsync(effectiveConnectionString, SchemaName, ct);
        var clearableTables = tableNames
            .Where(name => !name.Equals(MigrationJournalTableName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (clearableTables.Length == 0)
        {
            return new ProjectionClearResult(SchemaName, []);
        }

        await client.TruncateTablesAsync(effectiveConnectionString, SchemaName, clearableTables, ct);
        return new ProjectionClearResult(SchemaName, clearableTables);
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
}

internal sealed class NpgsqlProjectionStorageResetClient : IProjectionStorageResetClient
{
    public async Task<IReadOnlyList<string>> ListBaseTablesAsync(
        string connectionString,
        string schemaName,
        CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = @schemaName
              AND table_type = 'BASE TABLE'
            ORDER BY table_name;
            """;
        command.Parameters.AddWithValue("schemaName", schemaName);

        var tableNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    public async Task TruncateTablesAsync(
        string connectionString,
        string schemaName,
        IReadOnlyList<string> tableNames,
        CancellationToken ct)
    {
        if (tableNames.Count == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        var qualifiedTableNames = tableNames
            .Select(tableName => $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}");
        command.CommandText =
            $"TRUNCATE TABLE {string.Join(", ", qualifiedTableNames)} RESTART IDENTITY CASCADE;";

        await command.ExecuteNonQueryAsync(ct);
    }

    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

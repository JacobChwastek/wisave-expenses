using Microsoft.Extensions.Configuration;
using WiSave.Expenses.Console.Operations;

namespace WiSave.Expenses.Console.Tests.Operations;

public class ProjectionClearOperationsTests
{
    [Fact]
    public async Task RunAsync_uses_configuration_connection_string_when_override_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=config"
            })
            .Build();
        var client = new StubProjectionStorageResetClient(["accounts"]);
        var sut = new ProjectionClearOperations(configuration, client);

        await sut.RunAsync(null, CancellationToken.None);

        Assert.Equal("Host=config", client.ConnectionString);
    }

    [Fact]
    public async Task RunAsync_excludes_schema_versions_from_truncate()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=config"
            })
            .Build();
        var client = new StubProjectionStorageResetClient(["accounts", "SchemaVersions", "processed_messages"]);
        var sut = new ProjectionClearOperations(configuration, client);

        var result = await sut.RunAsync(null, CancellationToken.None);

        Assert.Equal(["accounts", "processed_messages"], client.TruncatedTables);
        Assert.Equal(["accounts", "processed_messages"], result.ClearedTables);
    }

    [Fact]
    public async Task RunAsync_returns_empty_result_when_only_schema_versions_exists()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=config"
            })
            .Build();
        var client = new StubProjectionStorageResetClient(["SchemaVersions"]);
        var sut = new ProjectionClearOperations(configuration, client);

        var result = await sut.RunAsync(null, CancellationToken.None);

        Assert.Empty(result.ClearedTables);
        Assert.Null(client.TruncatedTables);
    }

    private sealed class StubProjectionStorageResetClient(IEnumerable<string> tableNames) : IProjectionStorageResetClient
    {
        private readonly IReadOnlyList<string> discoveredTableNames = tableNames.ToArray();

        public string? ConnectionString { get; private set; }

        public IReadOnlyList<string>? TruncatedTables { get; private set; }

        public Task<IReadOnlyList<string>> ListBaseTablesAsync(string connectionString, string schemaName, CancellationToken ct)
        {
            ConnectionString = connectionString;
            return Task.FromResult(discoveredTableNames);
        }

        public Task TruncateTablesAsync(
            string connectionString,
            string schemaName,
            IReadOnlyList<string> tableNames,
            CancellationToken ct)
        {
            ConnectionString = connectionString;
            TruncatedTables = tableNames.ToArray();
            return Task.CompletedTask;
        }
    }
}

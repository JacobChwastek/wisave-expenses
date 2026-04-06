using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;

namespace WiSave.Expenses.Console.Commands;

internal sealed class DatabaseMigrateCommand(IDatabaseMigrationOperations migrationOperations) : IExpensesCommand
{
    private static readonly IReadOnlyList<CommandParameter> Parameters =
    [
        new("connection-string", "Override the default expenses connection string.", false)
    ];

    public string Name => "db-migrate";

    public string Description => "Apply expenses database migrations.";

    public IReadOnlyList<CommandParameter> ParameterDefinitions => Parameters;

    public async Task<CommandResult> ExecuteAsync(CommandExecutionContext context, CancellationToken ct)
    {
        var connectionString = context.GetArgument("connection-string");
        var message = await migrationOperations.RunAsync(connectionString, ct);

        return CommandResult.SuccessResult(message);
    }
}

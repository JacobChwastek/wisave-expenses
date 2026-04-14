using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Commands;

internal sealed class DatabaseMigrateCommand(
    IDatabaseMigrationOperations migrationOperations,
    IConsoleOutput consoleOutput) : IExpensesCommand
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
        if (context.AllowPrompting)
        {
            consoleOutput.WriteLine("This will apply Core and Projections database migrations.");
            consoleOutput.Write("Continue? [y/N]: ");

            var confirmation = consoleOutput.ReadLine()?.Trim();
            if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.SuccessResult("Database migration cancelled.");
            }
        }

        var connectionString = context.GetArgument("connection-string");
        var message = await migrationOperations.RunAsync(connectionString, ct);

        return CommandResult.SuccessResult(message);
    }
}

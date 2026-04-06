using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Commands;

internal sealed class ProjectionsClearCommand(
    IProjectionClearOperations clearOperations,
    IConsoleOutput consoleOutput) : IExpensesCommand
{
    private static readonly IReadOnlyList<CommandParameter> Parameters =
    [
        new("connection-string", "Override the default expenses connection string.", false)
    ];

    public string Name => "projections-clear";

    public string Description => "Delete rebuildable projection data from the projections schema.";

    public IReadOnlyList<CommandParameter> ParameterDefinitions => Parameters;

    public async Task<CommandResult> ExecuteAsync(CommandExecutionContext context, CancellationToken ct)
    {
        if (context.AllowPrompting)
        {
            consoleOutput.WriteLine("WARNING: This will permanently delete all projection read models and replay state.");
            consoleOutput.WriteLine("The projections schema will remain, but the projections worker must rebuild it.");
            consoleOutput.Write("Continue? [y/N]: ");

            var confirmation = consoleOutput.ReadLine()?.Trim();
            if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.SuccessResult("Projection clear cancelled.");
            }
        }

        try
        {
            var result = await clearOperations.RunAsync(context.GetArgument("connection-string"), ct);
            var details = result.ClearedTables
                .Select(tableName => $"cleared {result.SchemaName}.{tableName}")
                .ToArray();

            return CommandResult.SuccessResult(result.Format(), details);
        }
        catch (Exception ex)
        {
            return CommandResult.FailureResult($"Projection clear failed: {ex.Message}");
        }
    }
}

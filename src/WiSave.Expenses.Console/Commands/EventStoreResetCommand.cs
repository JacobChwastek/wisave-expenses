using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Commands;

internal sealed class EventStoreResetCommand(
    IEventStoreResetOperations resetOperations,
    IConsoleOutput consoleOutput) : IExpensesCommand
{
    private static readonly IReadOnlyList<CommandParameter> Parameters =
    [
        new("connection-string", "KurrentDB connection string (e.g. esdb://localhost:2113?tls=false).", true)
    ];

    public string Name => "eventstore-reset";

    public string Description => "Permanently tombstone all streams and delete all subscriptions in a KurrentDB instance.";

    public IReadOnlyList<CommandParameter> ParameterDefinitions => Parameters;

    public async Task<CommandResult> ExecuteAsync(CommandExecutionContext context, CancellationToken ct)
    {
        var connectionString = context.GetArgument("connection-string");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return CommandResult.FailureResult("--connection-string is required.");
        }

        if (context.AllowPrompting)
        {
            consoleOutput.WriteLine("WARNING: This will permanently tombstone ALL non-system streams");
            consoleOutput.WriteLine("and delete ALL persistent subscriptions. This is IRREVERSIBLE.");
            consoleOutput.WriteLine($"Target: {connectionString}");
            consoleOutput.Write("Type 'yes' to confirm: ");

            var confirmation = consoleOutput.ReadLine()?.Trim();
            if (!string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.SuccessResult("EventStore reset cancelled.");
            }
        }

        try
        {
            var result = await resetOperations.RunAsync(connectionString, consoleOutput, ct);

            return result.Errors.Count > 0
                ? CommandResult.FailureResult(result.Format())
                : CommandResult.SuccessResult(result.Format());
        }
        catch (Exception ex)
        {
            return CommandResult.FailureResult($"EventStore reset failed: {ex.Message}");
        }
    }
}

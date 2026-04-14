namespace WiSave.Expenses.Console.Execution;

internal interface IExpensesCommand
{
    string Name { get; }

    string Description { get; }

    IReadOnlyList<CommandParameter> ParameterDefinitions { get; }

    Task<CommandResult> ExecuteAsync(CommandExecutionContext context, CancellationToken ct);
}

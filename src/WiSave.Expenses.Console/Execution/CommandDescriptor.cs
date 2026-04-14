namespace WiSave.Expenses.Console.Execution;

internal sealed record CommandDescriptor(
    string Name,
    string Description,
    IReadOnlyList<CommandParameter> Parameters);

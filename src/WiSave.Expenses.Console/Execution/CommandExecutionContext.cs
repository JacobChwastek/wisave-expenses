namespace WiSave.Expenses.Console.Execution;

internal sealed class CommandExecutionContext(IReadOnlyDictionary<string, string?> arguments, bool allowPrompting = false)
{
    public IReadOnlyDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?>(arguments, StringComparer.OrdinalIgnoreCase);

    public bool AllowPrompting { get; } = allowPrompting;

    public string? GetArgument(string name)
        => Arguments.GetValueOrDefault(name);
}

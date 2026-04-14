namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public sealed class ContractEventTypeRegistry
{
    private readonly Dictionary<string, Type> _map;

    public ContractEventTypeRegistry()
    {
        var assembly = typeof(Contracts.Events.CommandFailed).Assembly;
        _map = assembly.GetExportedTypes()
            .Where(t => t.Namespace?.Contains(".Events.", StringComparison.Ordinal) == true)
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
    }

    public Type? Resolve(string eventTypeName) => _map.GetValueOrDefault(eventTypeName);
}

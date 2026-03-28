namespace WiSave.Expenses.Core.Domain.SharedKernel;

public abstract class AggregateRoot
{
    public string Id { get; protected set; } = string.Empty;
    public int Version { get; private set; } = -1;

    private readonly List<object> _uncommittedEvents = [];

    public IReadOnlyList<object> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    protected void RaiseEvent(object @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    public void ReplayEvents(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    protected abstract void Apply(object @event);
}

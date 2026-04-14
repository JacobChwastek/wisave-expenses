using WiSave.Expenses.Core.Infrastructure.EventStore;

namespace WiSave.Expenses.Worker.Domain.Tests.EventStore;

public class ContractEventTypeRegistryTests
{
    [Fact]
    public void Resolve_known_event_name_returns_type()
    {
        var sut = new ContractEventTypeRegistry();

        var type = sut.Resolve("AccountOpened");

        Assert.Equal("WiSave.Expenses.Contracts.Events.Accounts.AccountOpened", type?.FullName);
    }
}

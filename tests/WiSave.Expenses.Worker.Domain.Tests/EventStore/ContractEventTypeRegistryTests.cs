using WiSave.Expenses.Core.Infrastructure.EventStore;
using WiSave.Expenses.Contracts.Events.FundingAccounts;

namespace WiSave.Expenses.Worker.Domain.Tests.EventStore;

public class ContractEventTypeRegistryTests
{
    [Fact]
    public void Resolve_known_event_name_returns_type()
    {
        var sut = new ContractEventTypeRegistry();

        var type = sut.Resolve(nameof(FundingAccountOpened));

        Assert.Equal(typeof(FundingAccountOpened), type);
    }
}

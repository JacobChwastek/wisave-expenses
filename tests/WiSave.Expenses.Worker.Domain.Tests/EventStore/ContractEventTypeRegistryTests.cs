using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Framework.EventSourcing;

namespace WiSave.Expenses.Worker.Domain.Tests.EventStore;

public class ContractEventTypeRegistryTests
{
    [Fact]
    public void Resolve_known_event_name_returns_type()
    {
        var sut = AssemblyEventTypeRegistry.FromAssemblies(
            [typeof(FundingAccountOpened).Assembly],
            type => type.Namespace?.Contains(".Events.", StringComparison.Ordinal) == true);

        var type = sut.Resolve(nameof(FundingAccountOpened));

        Assert.Equal(typeof(FundingAccountOpened), type);
    }
}

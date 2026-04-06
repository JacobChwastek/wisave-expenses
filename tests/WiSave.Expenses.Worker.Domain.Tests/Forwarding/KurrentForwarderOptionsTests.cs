using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Configuration;

namespace WiSave.Expenses.Worker.Domain.Tests.Forwarding;

public class KurrentForwarderOptionsTests
{
    [Fact]
    public void Default_stream_prefixes_follow_split_account_streams()
    {
        var sut = new KurrentForwarderOptions();

        Assert.Equal(["funding-account-", "credit-card-account-", "budget-"], sut.StreamPrefixes);
    }
}

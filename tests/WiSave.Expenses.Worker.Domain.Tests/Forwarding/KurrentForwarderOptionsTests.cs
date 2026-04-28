using WiSave.Framework.EventSourcing.Kurrent.Configuration;

namespace WiSave.Expenses.Worker.Domain.Tests.Forwarding;

public class KurrentForwarderOptionsTests
{
    [Fact]
    public void Configured_stream_prefixes_follow_split_account_streams()
    {
        var sut = new KurrentForwarderOptions
        {
            StreamPrefixes = ["funding-account-"],
        };

        Assert.Equal(["funding-account-"], sut.StreamPrefixes);
    }
}

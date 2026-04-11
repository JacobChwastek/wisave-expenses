using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WiSave.Expenses.Worker.Domain.Forwarding;

namespace WiSave.Expenses.Worker.Domain.Tests.Forwarding;

public class KurrentSubscriptionBootstrapperTests
{
    [Fact]
    public async Task EnsureCreatedAsync_is_idempotent_when_group_exists()
    {
        var client = new FakePersistentSubscriptionClient
        {
            ThrowAlreadyExistsOnCreate = true,
        };

        var sut = new KurrentSubscriptionBootstrapper(
            client,
            Options.Create(new KurrentForwarderOptions
            {
                GroupName = "expenses-forwarder",
                FromStartWhenCreated = true,
            }),
            NullLogger<KurrentSubscriptionBootstrapper>.Instance);

        await sut.EnsureCreatedAsync(CancellationToken.None);

        Assert.Equal(1, client.CreateCalls);
    }

    [Fact]
    public async Task EnsureCreatedAsync_creates_group_with_dispatch_to_single_and_single_subscriber()
    {
        var client = new FakePersistentSubscriptionClient();

        var sut = new KurrentSubscriptionBootstrapper(
            client,
            Options.Create(new KurrentForwarderOptions
            {
                GroupName = "expenses-forwarder",
                FromStartWhenCreated = true,
                MaxSubscriberCount = 1,
                ConsumerStrategyName = "DispatchToSingle",
            }),
            NullLogger<KurrentSubscriptionBootstrapper>.Instance);

        await sut.EnsureCreatedAsync(CancellationToken.None);

        Assert.NotNull(client.LastCreateSettings);
        Assert.True(client.LastCreateSettings!.FromStartWhenCreated);
        Assert.Equal(1, client.LastCreateSettings.MaxSubscriberCount);
        Assert.Equal("DispatchToSingle", client.LastCreateSettings.ConsumerStrategyName);
    }
}

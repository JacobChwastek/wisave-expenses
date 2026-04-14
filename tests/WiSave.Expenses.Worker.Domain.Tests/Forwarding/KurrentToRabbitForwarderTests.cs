using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WiSave.Expenses.Contracts.Events.Accounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Infrastructure.EventStore;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Configuration;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Hosting;
using WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.PersistentSubscriptions;

namespace WiSave.Expenses.Worker.Domain.Tests.Forwarding;

public class KurrentToRabbitForwarderTests
{
    [Fact]
    public void Hosted_forwarder_registration_validates_with_scoped_publish_endpoint()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<KurrentForwarderOptions>().Configure(options =>
        {
            options.GroupName = "expenses-forwarder";
            options.StreamPrefixes = ["account-", "expense-", "budget-"];
        });
        services.AddSingleton<IKurrentPersistentSubscriptionClient, FakePersistentSubscriptionClient>();
        services.AddSingleton<KurrentSubscriptionBootstrapper>();
        services.AddSingleton<ContractEventTypeRegistry>();
        services.AddScoped<IPublishEndpoint>(_ => new RecordingPublishEndpoint());
        services.AddHostedService<KurrentToRabbitForwarder>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        var hostedService = provider.GetService<IHostedService>();
        Assert.NotNull(hostedService);
    }

    [Fact]
    public async Task HandleEventAsync_publishes_known_contract_event_and_acks()
    {
        var publishEndpoint = new RecordingPublishEndpoint();
        var registry = new ContractEventTypeRegistry();
        var options = Options.Create(new KurrentForwarderOptions
        {
            GroupName = "expenses-forwarder",
            StreamPrefixes = ["account-", "expense-", "budget-"],
        });

        var sut = new KurrentToRabbitForwarder(
            client: new FakePersistentSubscriptionClient(),
            bootstrapper: new KurrentSubscriptionBootstrapper(
                new FakePersistentSubscriptionClient(),
                options,
                NullLogger<KurrentSubscriptionBootstrapper>.Instance),
            CreateScopeFactory(publishEndpoint),
            registry,
            options,
            NullLogger<KurrentToRabbitForwarder>.Instance);

        var actions = new FakeSubscriptionActions();
        var message = new AccountOpened(
            AccountId: "acc-1",
            UserId: "user-1",
            Name: "Checking",
            Type: AccountType.BankAccount,
            Currency: Currency.USD,
            Balance: 100m,
            LinkedBankAccountId: null,
            CreditLimit: null,
            BillingCycleDay: null,
            Color: null,
            LastFourDigits: null,
            Timestamp: DateTimeOffset.UtcNow);

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                EventId: Guid.NewGuid(),
                EventType: "AccountOpened",
                StreamId: "account-acc-1",
                Data: JsonSerializer.SerializeToUtf8Bytes(message),
                Actions: actions),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Single(publishEndpoint.Published);
        Assert.IsType<AccountOpened>(publishEndpoint.Published[0]);
        Assert.Equal(1, actions.AckCalls);
        Assert.Equal(0, actions.RetryCalls);
        Assert.Equal(0, actions.SkipCalls);
    }

    [Fact]
    public async Task HandleEventAsync_parks_unknown_event_on_expenses_stream()
    {
        var publishEndpoint = new RecordingPublishEndpoint();
        var registry = new ContractEventTypeRegistry();
        var options = Options.Create(new KurrentForwarderOptions
        {
            GroupName = "expenses-forwarder",
            StreamPrefixes = ["account-", "expense-", "budget-"],
        });

        var sut = new KurrentToRabbitForwarder(
            client: new FakePersistentSubscriptionClient(),
            bootstrapper: new KurrentSubscriptionBootstrapper(
                new FakePersistentSubscriptionClient(),
                options,
                NullLogger<KurrentSubscriptionBootstrapper>.Instance),
            CreateScopeFactory(publishEndpoint),
            registry,
            options,
            NullLogger<KurrentToRabbitForwarder>.Instance);

        var actions = new FakeSubscriptionActions();

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                EventId: Guid.NewGuid(),
                EventType: "UnknownEvent",
                StreamId: "account-acc-1",
                Data: "{}"u8.ToArray(),
                Actions: actions),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Empty(publishEndpoint.Published);
        Assert.Equal(0, actions.AckCalls);
        Assert.Equal(0, actions.RetryCalls);
        Assert.Equal(1, actions.ParkCalls);
    }

    [Fact]
    public async Task HandleEventAsync_skips_event_from_out_of_scope_stream()
    {
        var publishEndpoint = new RecordingPublishEndpoint();
        var registry = new ContractEventTypeRegistry();
        var options = Options.Create(new KurrentForwarderOptions
        {
            GroupName = "expenses-forwarder",
            StreamPrefixes = ["account-", "expense-", "budget-"],
        });

        var sut = new KurrentToRabbitForwarder(
            client: new FakePersistentSubscriptionClient(),
            bootstrapper: new KurrentSubscriptionBootstrapper(
                new FakePersistentSubscriptionClient(),
                options,
                NullLogger<KurrentSubscriptionBootstrapper>.Instance),
            CreateScopeFactory(publishEndpoint),
            registry,
            options,
            NullLogger<KurrentToRabbitForwarder>.Instance);

        var actions = new FakeSubscriptionActions();

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                EventId: Guid.NewGuid(),
                EventType: "AccountOpened",
                StreamId: "other-service-1",
                Data: "{}"u8.ToArray(),
                Actions: actions),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Empty(publishEndpoint.Published);
        Assert.Equal(0, actions.AckCalls);
        Assert.Equal(1, actions.SkipCalls);
    }

    [Fact]
    public async Task HandleEventAsync_nacks_retry_when_publish_fails()
    {
        var registry = new ContractEventTypeRegistry();
        var options = Options.Create(new KurrentForwarderOptions
        {
            GroupName = "expenses-forwarder",
            StreamPrefixes = ["account-", "expense-", "budget-"],
        });

        var sut = new KurrentToRabbitForwarder(
            client: new FakePersistentSubscriptionClient(),
            bootstrapper: new KurrentSubscriptionBootstrapper(
                new FakePersistentSubscriptionClient(),
                options,
                NullLogger<KurrentSubscriptionBootstrapper>.Instance),
            scopeFactory: CreateScopeFactory(new FailingPublishEndpoint()),
            registry,
            options,
            NullLogger<KurrentToRabbitForwarder>.Instance);

        var actions = new FakeSubscriptionActions();
        var message = new AccountOpened(
            AccountId: "acc-1",
            UserId: "user-1",
            Name: "Checking",
            Type: AccountType.BankAccount,
            Currency: Currency.USD,
            Balance: 100m,
            LinkedBankAccountId: null,
            CreditLimit: null,
            BillingCycleDay: null,
            Color: null,
            LastFourDigits: null,
            Timestamp: DateTimeOffset.UtcNow);

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                EventId: Guid.NewGuid(),
                EventType: "AccountOpened",
                StreamId: "account-acc-1",
                Data: JsonSerializer.SerializeToUtf8Bytes(message),
                Actions: actions),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(0, actions.AckCalls);
        Assert.Equal(1, actions.RetryCalls);
    }

    private static IServiceScopeFactory CreateScopeFactory(IPublishEndpoint publishEndpoint)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => publishEndpoint);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

internal sealed class FakePersistentSubscriptionClient : IKurrentPersistentSubscriptionClient
{
    public int CreateCalls { get; private set; }
    public bool ThrowAlreadyExistsOnCreate { get; set; }
    public KurrentPersistentSubscriptionCreateOptions? LastCreateSettings { get; private set; }

    public Task CreateToAllAsync(string groupName, KurrentPersistentSubscriptionCreateOptions options, CancellationToken ct)
    {
        CreateCalls++;
        LastCreateSettings = options;
        if (ThrowAlreadyExistsOnCreate)
        {
            throw new KurrentPersistentSubscriptionAlreadyExistsException(groupName);
        }

        return Task.CompletedTask;
    }

    public Task<IKurrentPersistentSubscription> SubscribeToAllAsync(string groupName, CancellationToken ct) =>
        Task.FromResult<IKurrentPersistentSubscription>(new FakePersistentSubscription());

    private sealed class FakePersistentSubscription : IKurrentPersistentSubscription
    {
        public IAsyncEnumerable<KurrentPersistentSubscriptionMessage> Messages => ReadMessagesAsync();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static async IAsyncEnumerable<KurrentPersistentSubscriptionMessage> ReadMessagesAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

internal sealed class FakeSubscriptionActions : IKurrentSubscriptionActions
{
    public int AckCalls { get; private set; }
    public int RetryCalls { get; private set; }
    public int ParkCalls { get; private set; }
    public int SkipCalls { get; private set; }

    public Task AckAsync(CancellationToken ct)
    {
        AckCalls++;
        return Task.CompletedTask;
    }

    public Task ParkAsync(string reason, CancellationToken ct)
    {
        ParkCalls++;
        return Task.CompletedTask;
    }

    public Task RetryAsync(string reason, CancellationToken ct)
    {
        RetryCalls++;
        return Task.CompletedTask;
    }

    public Task SkipAsync(string reason, CancellationToken ct)
    {
        SkipCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPublishEndpoint : IPublishEndpoint
{
    public List<object> Published { get; } = [];

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(values);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(values);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(values);
        return Task.CompletedTask;
    }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FailingPublishEndpoint : IPublishEndpoint
{
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish(object message, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Rabbit unavailable");
    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Rabbit unavailable");
}

using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Application.Funding.Handlers;
using WiSave.Expenses.Core.Domain.Funding;

namespace WiSave.Expenses.Core.Application.Tests.Funding;

public class FundingAccountCommandHandlerTests
{
    [Fact]
    public async Task OpenFundingAccount_saves_one_funding_account_stream()
    {
        var repository = new InMemoryFundingAccountRepository();
        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new OpenFundingAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name: "Cash",
                Kind: FundingAccountKind.Cash,
                Currency: Currency.PLN,
                OpeningBalance: 250m,
                Color: "#16a34a"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            Assert.Equal(1, repository.StreamCount);
            var saved = Assert.Single(repository.LoadAll());
            Assert.Equal("11111111-1111-1111-1111-111111111111", saved.UserId.Value);
            Assert.Equal(250m, saved.Balance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task UpdateFundingAccount_persists_changed_state()
    {
        var repository = new InMemoryFundingAccountRepository();
        await repository.SaveAsync(FundingAccount.Open(
            new FundingAccountId("funding-1"),
            new UserId("user-1"),
            "Cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            100m,
            null));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new UpdateFundingAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                FundingAccountId: "funding-1",
                Name: "Savings",
                Kind: FundingAccountKind.BankAccount,
                Currency: Currency.EUR,
                Color: "#2563eb"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var updated = await repository.LoadAsync(new FundingAccountId("funding-1"));
            Assert.NotNull(updated);
            Assert.Equal("Savings", updated.Name);
            Assert.Equal(FundingAccountKind.BankAccount, updated.Kind);
            Assert.Equal(Currency.EUR, updated.Currency);
            Assert.Equal("#2563eb", updated.Color);
            Assert.Equal(100m, updated.Balance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task CloseFundingAccount_persists_inactive_state()
    {
        var repository = new InMemoryFundingAccountRepository();
        await repository.SaveAsync(FundingAccount.Open(
            new FundingAccountId("funding-1"),
            new UserId("user-1"),
            "Cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            100m,
            null));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new CloseFundingAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                FundingAccountId: "funding-1"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var closed = await repository.LoadAsync(new FundingAccountId("funding-1"));
            Assert.NotNull(closed);
            Assert.False(closed.IsActive);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AddFundingPaymentInstrument_persists_child_entity()
    {
        var repository = new InMemoryFundingAccountRepository();
        await repository.SaveAsync(FundingAccount.Open(
            new FundingAccountId("funding-1"),
            new UserId("user-1"),
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            100m,
            null));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new AddFundingPaymentInstrument(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                FundingAccountId: "funding-1",
                Name: "mBank debit",
                Kind: PaymentInstrumentKind.DebitCard,
                LastFourDigits: "4532",
                Network: "Visa",
                Color: "#0f766e"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var account = await repository.LoadAsync(new FundingAccountId("funding-1"));
            Assert.NotNull(account);
            var instrument = Assert.Single(account.PaymentInstruments);
            Assert.Equal("mBank debit", instrument.Name);
            Assert.True(instrument.IsActive);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PostFundingTransfer_persists_transfer_event_and_reduces_balance()
    {
        var repository = new InMemoryFundingAccountRepository();
        await repository.SaveAsync(FundingAccount.Open(
            new FundingAccountId("funding-1"),
            new UserId("user-1"),
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            100m,
            null));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new PostFundingTransfer(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                FundingAccountId: "funding-1",
                TransferId: "transfer-1",
                Amount: 35m,
                PostedAtUtc: DateTimeOffset.Parse("2026-05-16T10:00:00Z")));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var account = await repository.LoadAsync(new FundingAccountId("funding-1"));
            Assert.NotNull(account);
            Assert.Equal(65m, account.Balance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task UpdateFundingAccount_publishes_command_failed_when_account_is_missing()
    {
        var repository = new InMemoryFundingAccountRepository();
        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new UpdateFundingAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                FundingAccountId: "missing",
                Name: "Savings",
                Kind: FundingAccountKind.BankAccount,
                Currency: Currency.EUR,
                Color: null));

            Assert.True(await harness.Published.Any<CommandFailed>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task CloseFundingAccount_publishes_command_failed_when_user_does_not_own_account()
    {
        var repository = new InMemoryFundingAccountRepository();
        await repository.SaveAsync(FundingAccount.Open(
            new FundingAccountId("funding-1"),
            new UserId("user-1"),
            "Cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            100m,
            null));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new CloseFundingAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "other-user",
                FundingAccountId: "funding-1"));

            Assert.True(await harness.Published.Any<CommandFailed>());
            var account = await repository.LoadAsync(new FundingAccountId("funding-1"));
            Assert.NotNull(account);
            Assert.True(account.IsActive);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider BuildProvider(InMemoryFundingAccountRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton<IAggregateRepository<FundingAccount, FundingAccountId>>(repository);
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<OpenFundingAccountHandler>();
            cfg.AddConsumer<UpdateFundingAccountHandler>();
            cfg.AddConsumer<CloseFundingAccountHandler>();
            cfg.AddConsumer<AddFundingPaymentInstrumentHandler>();
            cfg.AddConsumer<UpdateFundingPaymentInstrumentHandler>();
            cfg.AddConsumer<RemoveFundingPaymentInstrumentHandler>();
            cfg.AddConsumer<PostFundingTransferHandler>();
        });
        return services.BuildServiceProvider(true);
    }

    private sealed class InMemoryFundingAccountRepository : IAggregateRepository<FundingAccount, FundingAccountId>
    {
        private readonly Dictionary<string, List<object>> _streams = new();

        public int StreamCount => _streams.Count;

        public IReadOnlyList<FundingAccount> LoadAll() =>
            _streams.Values.Select(events =>
            {
                var aggregate = new FundingAccount();
                aggregate.ReplayEvents(events);
                return aggregate;
            }).ToArray();

        public Task<FundingAccount?> LoadAsync(FundingAccountId id, CancellationToken ct = default)
        {
            var streamId = FundingAccount.ToStreamId(id);
            if (!_streams.TryGetValue(streamId, out var events))
                return Task.FromResult<FundingAccount?>(null);

            var aggregate = new FundingAccount();
            aggregate.ReplayEvents(events);
            return Task.FromResult<FundingAccount?>(aggregate);
        }

        public Task SaveAsync(FundingAccount aggregate, CancellationToken ct = default)
        {
            var streamId = FundingAccount.ToStreamId(aggregate.Id);
            if (!_streams.TryGetValue(streamId, out var events))
            {
                events = [];
                _streams[streamId] = events;
            }

            events.AddRange(aggregate.GetUncommittedEvents());
            aggregate.ClearUncommittedEvents();
            return Task.CompletedTask;
        }
    }
}

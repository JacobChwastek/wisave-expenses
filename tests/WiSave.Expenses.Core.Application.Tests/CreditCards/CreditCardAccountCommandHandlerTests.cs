using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Events;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Core.Application.CreditCards.Handlers;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.Funding;

namespace WiSave.Expenses.Core.Application.Tests.CreditCards;

public class CreditCardAccountCommandHandlerTests
{
    [Fact]
    public async Task OpenCreditCardAccount_saves_valid_card()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        var lookup = StubFundingAccountLookup.With("funding-1", "11111111-1111-1111-1111-111111111111", Currency.PLN);
        await using var provider = BuildProvider(repository, lookup);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new OpenCreditCardAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name: "mBank Visa",
                Currency: Currency.PLN,
                SettlementAccountId: "funding-1",
                BankProvider: BankProvider.MBank,
                ProductCode: "STANDARD",
                CreditLimit: 5000m,
                StatementClosingDay: 20,
                GracePeriodDays: 24,
                Color: "#0f766e",
                LastFourDigits: "1234"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            Assert.Equal(1, repository.StreamCount);
            var saved = Assert.Single(repository.LoadAll());
            Assert.Equal("11111111-1111-1111-1111-111111111111", saved.UserId.Value);
            Assert.Equal("mBank Visa", saved.Name);
            Assert.Equal(Currency.PLN, saved.Currency);
            Assert.Equal("funding-1", saved.SettlementAccountId.Value);
            Assert.True(saved.IsActive);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task IssueCreditCardStatement_loads_account_computes_policy_and_saves_event()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var card = await repository.LoadAsync(new CreditCardAccountId("card-1"));
        SeedState(card!,
            activeStatementBalance: 0m,
            activeStatementMinimumPaymentDue: 0m,
            activeStatementPeriodCloseDate: null,
            activeStatementDueDate: null,
            unbilledBalance: 1200m);
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new IssueCreditCardStatement(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                CreditCardAccountId: "card-1",
                CalculationDate: new DateOnly(2026, 4, 20)));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var updated = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(updated);
            Assert.Equal(1200m, updated.ActiveStatementBalance);
            Assert.Equal(1200m, updated.ActiveStatementOutstandingBalance);
            Assert.Equal(60m, updated.ActiveStatementMinimumPaymentDue);
            Assert.Equal(new DateOnly(2026, 5, 14), updated.ActiveStatementDueDate);
            Assert.Equal(0m, updated.UnbilledBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FundingTransferPosted_with_target_credit_card_applies_settlement()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var card = await repository.LoadAsync(new CreditCardAccountId("card-1"));
        SeedState(card!,
            activeStatementBalance: 300m,
            activeStatementMinimumPaymentDue: 15m,
            activeStatementPeriodCloseDate: new DateOnly(2026, 3, 20),
            activeStatementDueDate: new DateOnly(2026, 4, 13),
            unbilledBalance: 0m);
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "funding-1",
                UserId: "user-1",
                TransferId: "transfer-1",
                TargetCreditCardAccountId: "card-1",
                StatementId: null,
                Amount: 125m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-10T10:00:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-10T10:00:00Z")));

            Assert.True(await harness.Consumed.Any<FundingTransferPosted>());
            var updated = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(updated);
            Assert.Equal(175m, updated.ActiveStatementOutstandingBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FundingTransferPosted_ignores_cross_user_or_wrong_funding_account_event()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var card = await repository.LoadAsync(new CreditCardAccountId("card-1"));
        SeedState(card!,
            activeStatementBalance: 300m,
            activeStatementMinimumPaymentDue: 15m,
            activeStatementPeriodCloseDate: new DateOnly(2026, 3, 20),
            activeStatementDueDate: new DateOnly(2026, 4, 13),
            unbilledBalance: 0m);
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "other-funding",
                UserId: "user-1",
                TransferId: "transfer-1",
                TargetCreditCardAccountId: "card-1",
                StatementId: null,
                Amount: 125m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-10T10:00:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-10T10:00:00Z")));
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "funding-1",
                UserId: "other-user",
                TransferId: "transfer-2",
                TargetCreditCardAccountId: "card-1",
                StatementId: null,
                Amount: 125m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-10T10:00:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-10T10:00:00Z")));

            var unchanged = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(unchanged);
            Assert.Equal(300m, unchanged.ActiveStatementOutstandingBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FundingTransferPosted_with_statement_id_applies_to_requested_statement()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var card = await repository.LoadAsync(new CreditCardAccountId("card-1"));
        IssueStatement(card!, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 2, 21),
            PeriodTo: new DateOnly(2026, 3, 20),
            StatementDate: new DateOnly(2026, 3, 20),
            DueDate: new DateOnly(2026, 4, 13),
            StatementBalance: 300m,
            MinimumPaymentDue: 15m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        IssueStatement(card!, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 3, 21),
            PeriodTo: new DateOnly(2026, 4, 20),
            StatementDate: new DateOnly(2026, 4, 20),
            DueDate: new DateOnly(2026, 5, 14),
            StatementBalance: 200m,
            MinimumPaymentDue: 10m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "funding-1",
                UserId: "user-1",
                TransferId: "transfer-1",
                TargetCreditCardAccountId: "card-1",
                StatementId: "stmt-2",
                Amount: 50m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-21T10:00:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-21T10:00:00Z")));

            Assert.True(await harness.Consumed.Any<FundingTransferPosted>());
            var updated = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(updated);
            var snapshots = updated.GetOpenStatementSnapshots().OrderBy(x => x.StatementId).ToArray();
            Assert.Equal(300m, snapshots.Single(x => x.StatementId == "stmt-1").OutstandingBalance);
            Assert.Equal(150m, snapshots.Single(x => x.StatementId == "stmt-2").OutstandingBalance);
            Assert.Equal(150m, updated.ActiveStatementOutstandingBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FundingTransferPosted_duplicate_different_settlement_does_not_poison_consumer_or_mutate()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var card = await repository.LoadAsync(new CreditCardAccountId("card-1"));
        SeedState(card!,
            activeStatementBalance: 300m,
            activeStatementMinimumPaymentDue: 15m,
            activeStatementPeriodCloseDate: new DateOnly(2026, 3, 20),
            activeStatementDueDate: new DateOnly(2026, 4, 13),
            unbilledBalance: 0m);
        card!.ApplySettlementTransfer(
            new TransferId("transfer-1"),
            50m,
            DateTimeOffset.Parse("2026-04-10T10:00:00Z"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 50m)]);
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "funding-1",
                UserId: "user-1",
                TransferId: "transfer-1",
                TargetCreditCardAccountId: "card-1",
                StatementId: null,
                Amount: 100m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-10T10:01:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-10T10:01:00Z")));

            Assert.True(await harness.Consumed.Any<FundingTransferPosted>());
            var unchanged = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(unchanged);
            Assert.Equal(250m, unchanged.ActiveStatementOutstandingBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FundingTransferPosted_unexpected_domain_failure_is_faulted()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        var card = OpenCard();
        SeedState(card,
            new CreditCardStatementId("stmt-seeded"),
            activeStatementBalance: 300m,
            activeStatementMinimumPaymentDue: 15m,
            activeStatementPeriodCloseDate: new DateOnly(2026, 3, 20),
            activeStatementDueDate: new DateOnly(2026, 4, 13),
            unbilledBalance: 0m,
            DateTimeOffset.Parse("2026-04-01T10:00:00Z"));
        card.Close(DateTimeOffset.Parse("2026-04-02T10:00:00Z"));
        await repository.SaveAsync(card!);

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new FundingTransferPosted(
                FundingAccountId: "funding-1",
                UserId: "user-1",
                TransferId: "transfer-1",
                TargetCreditCardAccountId: "card-1",
                StatementId: null,
                Amount: 100m,
                PostedAtUtc: DateTimeOffset.Parse("2026-04-10T10:01:00Z"),
                Timestamp: DateTimeOffset.Parse("2026-04-10T10:01:00Z")));

            Assert.True(await harness.Published.Any<Fault<FundingTransferPosted>>());
            var unchanged = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(unchanged);
            Assert.False(unchanged.IsActive);
            Assert.Equal(300m, unchanged.ActiveStatementOutstandingBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task UpdateCreditCardAccount_reconfigures_existing_card()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        var lookup = StubFundingAccountLookup.With("funding-2", "user-1", Currency.EUR);
        await using var provider = BuildProvider(repository, lookup);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new UpdateCreditCardAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                CreditCardAccountId: "card-1",
                Name: "mBank Visa EUR",
                Currency: Currency.EUR,
                SettlementAccountId: "funding-2",
                BankProvider: BankProvider.MBank,
                ProductCode: "STANDARD",
                CreditLimit: 7000m,
                StatementClosingDay: 16,
                GracePeriodDays: 25,
                Color: "#1d4ed8",
                LastFourDigits: "9876"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var updated = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(updated);
            Assert.Equal("mBank Visa EUR", updated.Name);
            Assert.Equal(Currency.EUR, updated.Currency);
            Assert.Equal("funding-2", updated.SettlementAccountId.Value);
            Assert.Equal(7000m, updated.CreditLimit);
            Assert.Equal(16, updated.StatementClosingDay.Value);
            Assert.Equal(25, updated.GracePeriodDays.Value);
            Assert.Equal("#1d4ed8", updated.Color);
            Assert.Equal("9876", updated.LastFourDigits);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task SeedCreditCardState_sets_initial_snapshot()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new SeedCreditCardState(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                CreditCardAccountId: "card-1",
                ActiveStatementBalance: 300m,
                ActiveStatementMinimumPaymentDue: 15m,
                ActiveStatementPeriodCloseDate: new DateOnly(2026, 3, 20),
                ActiveStatementDueDate: new DateOnly(2026, 4, 13),
                UnbilledBalance: 25m));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var seeded = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(seeded);
            Assert.Equal(300m, seeded.ActiveStatementBalance);
            Assert.Equal(300m, seeded.ActiveStatementOutstandingBalance);
            Assert.Equal(15m, seeded.ActiveStatementMinimumPaymentDue);
            Assert.Equal(25m, seeded.UnbilledBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task CloseCreditCardAccount_marks_card_inactive()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());
        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new CloseCreditCardAccount(
                CorrelationId: Guid.NewGuid(),
                UserId: "user-1",
                CreditCardAccountId: "card-1"));

            Assert.False(await harness.Published.Any<CommandFailed>());
            var closed = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(closed);
            Assert.False(closed.IsActive);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task IssueCreditCardStatement_publishes_command_failed_when_user_does_not_own_card()
    {
        var repository = new InMemoryCreditCardAccountRepository();
        await repository.SaveAsync(OpenCard());

        await using var provider = BuildProvider(repository, StubFundingAccountLookup.Empty);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new IssueCreditCardStatement(
                CorrelationId: Guid.NewGuid(),
                UserId: "other-user",
                CreditCardAccountId: "card-1",
                CalculationDate: new DateOnly(2026, 4, 20)));

            Assert.True(await harness.Published.Any<CommandFailed>());
            var unchanged = await repository.LoadAsync(new CreditCardAccountId("card-1"));
            Assert.NotNull(unchanged);
            Assert.Null(unchanged.ActiveStatementBalance);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider BuildProvider(
        InMemoryCreditCardAccountRepository repository,
        IFundingAccountLookup fundingAccountLookup)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton<IAggregateRepository<CreditCardAccount, CreditCardAccountId>>(repository);
        services.AddSingleton(fundingAccountLookup);
        services.AddSingleton<ICreditCardStatementPolicyResolver, StatementPolicyResolver>();
        services.AddSingleton<ICreditCardPaymentAllocationPolicy, OldestStatementFirstCreditCardPaymentAllocationPolicy>();
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<OpenCreditCardAccountHandler>();
            cfg.AddConsumer<UpdateCreditCardAccountHandler>();
            cfg.AddConsumer<CloseCreditCardAccountHandler>();
            cfg.AddConsumer<SeedCreditCardStateHandler>();
            cfg.AddConsumer<IssueCreditCardStatementHandler>();
            cfg.AddConsumer<ApplySettlementTransferHandler>();
        });
        return services.BuildServiceProvider(true);
    }

    private static CreditCardAccount OpenCard() =>
        CreditCardAccount.Open(
            new CreditCardAccountId("card-1"),
            new UserId("user-1"),
            "mBank Visa",
            Currency.PLN,
            new FundingAccountId("funding-1"),
            BankProvider.MBank,
            "STANDARD",
            5000m,
            20,
            24,
            "#0f766e",
            "1234",
            TestOccurredAtUtc);

    private static DateTimeOffset TestOccurredAtUtc { get; } =
        DateTimeOffset.Parse("2026-04-01T10:00:00Z");

    private static void SeedState(
        CreditCardAccount account,
        decimal activeStatementBalance,
        decimal activeStatementMinimumPaymentDue,
        DateOnly? activeStatementPeriodCloseDate,
        DateOnly? activeStatementDueDate,
        decimal unbilledBalance)
    {
        var statementId = activeStatementBalance > 0m
            ? new CreditCardStatementId($"stmt-{account.GetOpenStatementSnapshots().Count + 1}")
            : null;

        account.SeedState(
            statementId,
            activeStatementBalance,
            activeStatementMinimumPaymentDue,
            activeStatementPeriodCloseDate,
            activeStatementDueDate,
            unbilledBalance,
            TestOccurredAtUtc);
    }

    private static void SeedState(
        CreditCardAccount account,
        CreditCardStatementId? activeStatementId,
        decimal activeStatementBalance,
        decimal activeStatementMinimumPaymentDue,
        DateOnly? activeStatementPeriodCloseDate,
        DateOnly? activeStatementDueDate,
        decimal unbilledBalance,
        DateTimeOffset occurredAtUtc)
    {
        account.SeedState(
            activeStatementId,
            activeStatementBalance,
            activeStatementMinimumPaymentDue,
            activeStatementPeriodCloseDate,
            activeStatementDueDate,
            unbilledBalance,
            occurredAtUtc);
    }

    private static void IssueStatement(CreditCardAccount account, CreditCardStatementComputation computation)
    {
        account.IssueStatement(
            new CreditCardStatementId($"stmt-{account.GetOpenStatementSnapshots().Count + 1}"),
            computation,
            TestOccurredAtUtc);
    }

    private sealed class StubFundingAccountLookup : IFundingAccountLookup
    {
        private readonly Dictionary<string, FundingAccountCandidate> _accounts = new(StringComparer.Ordinal);

        public static IFundingAccountLookup Empty { get; } = new StubFundingAccountLookup();

        public static IFundingAccountLookup With(
            string accountId,
            string userId,
            Currency currency,
            bool isActive = true)
        {
            var lookup = new StubFundingAccountLookup();
            lookup._accounts[accountId] = new FundingAccountCandidate(accountId, userId, currency, isActive);
            return lookup;
        }

        public Task<FundingAccountCandidate?> GetAsync(string fundingAccountId, CancellationToken ct = default) =>
            Task.FromResult(_accounts.GetValueOrDefault(fundingAccountId));
    }

    private sealed class InMemoryCreditCardAccountRepository
        : IAggregateRepository<CreditCardAccount, CreditCardAccountId>
    {
        private readonly Dictionary<string, List<object>> _streams = new();

        public int StreamCount => _streams.Count;

        public IReadOnlyList<CreditCardAccount> LoadAll() =>
            _streams.Values.Select(events =>
            {
                var aggregate = new CreditCardAccount();
                aggregate.ReplayEvents(events);
                return aggregate;
            }).ToArray();

        public Task<CreditCardAccount?> LoadAsync(CreditCardAccountId id, CancellationToken ct = default)
        {
            var streamId = CreditCardAccount.ToStreamId(id);
            if (!_streams.TryGetValue(streamId, out var events))
                return Task.FromResult<CreditCardAccount?>(null);

            var aggregate = new CreditCardAccount();
            aggregate.ReplayEvents(events);
            return Task.FromResult<CreditCardAccount?>(aggregate);
        }

        public Task SaveAsync(CreditCardAccount aggregate, CancellationToken ct = default)
        {
            var streamId = CreditCardAccount.ToStreamId(aggregate.Id);
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

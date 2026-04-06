using WiSave.Expenses.Contracts.Events.CreditCards;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.CreditCards;
using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.Tests.CreditCards;

public class CreditCardAccountTests
{
    [Fact]
    public void Statement_financials_negative_statement_balance_throws_dedicated_exception()
    {
        var ex = Assert.Throws<StatementBalanceCannotBeNegativeException>(() => new StatementFinancials(
            statementBalance: -1m,
            minimumPaymentDue: 0m,
            unbilledBalanceAfterIssue: 0m,
            outstandingBalance: 0m));

        Assert.IsAssignableFrom<DomainException>(ex);
        Assert.Equal("Statement balance cannot be negative.", ex.Message);
    }

    [Fact]
    public void Statement_payment_application_requires_positive_rich_amount()
    {
        var ex = Assert.Throws<PaymentApplicationAmountMustBeGreaterThanZeroException>(() => new StatementPaymentApplication(
            new TransferId("trf-1"),
            0m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00")));

        Assert.Equal("Payment application amount must be greater than zero.", ex.Message);
    }

    [Fact]
    public void Statement_payment_application_exposes_amount_as_value_object()
    {
        var application = new StatementPaymentApplication(
            new TransferId("trf-1"),
            new StatementPaymentAmount(50m),
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"));

        Assert.Equal(50m, application.Amount.Value);
    }

    [Fact]
    public void Open_accepts_valid_statement_term_value_objects()
    {
        var account = CreditCardAccount.Open(
            new CreditCardAccountId("card-1"),
            new UserId("user-1"),
            "mBank Visa",
            Currency.PLN,
            new FundingAccountId("fund-1"),
            BankProvider.MBank,
            "STANDARD",
            12000m,
            new StatementClosingDay(16),
            new GracePeriodDays(24),
            "#f59e0b",
            "4532",
            CreditCardAccountTestExtensions.TestOccurredAtUtc);

        Assert.Equal(16, account.StatementClosingDay.Value);
        Assert.Equal(24, account.GracePeriodDays.Value);
    }

    [Fact]
    public void Open_exposes_no_active_statement_snapshot()
    {
        var account = OpenDefault();

        Assert.Null(account.ActiveStatementBalance);
        Assert.Null(account.ActiveStatementOutstandingBalance);
        Assert.Null(account.ActiveStatementMinimumPaymentDue);
        Assert.Null(account.ActiveStatementDueDate);
        Assert.Null(account.ActiveStatementPeriodCloseDate);
        Assert.Equal(0m, account.UnbilledBalance);
    }

    [Fact]
    public void Seed_zero_active_statement_leaves_active_snapshot_null_and_sets_unbilled_balance()
    {
        var account = OpenDefault();

        account.SeedState(
            0m,
            0m,
            null,
            null,
            10458m);

        Assert.Null(account.ActiveStatementBalance);
        Assert.Null(account.ActiveStatementOutstandingBalance);
        Assert.Null(account.ActiveStatementMinimumPaymentDue);
        Assert.Null(account.ActiveStatementDueDate);
        Assert.Null(account.ActiveStatementPeriodCloseDate);
        Assert.Equal(10458m, account.UnbilledBalance);
        Assert.Empty(account.GetOpenStatementSnapshots());
    }

    [Fact]
    public void Seed_zero_active_statement_rejects_minimum_due_or_dates()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<ZeroActiveStatementSeedCannotIncludeMinimumPaymentOrDatesException>(() => account.SeedState(
            0m,
            1m,
            null,
            null,
            10458m));

        Assert.Equal("Zero active statement seed cannot include minimum payment or dates.", ex.Message);
        Assert.DoesNotContain(account.GetUncommittedEvents(), x => x is CreditCardStateSeeded);
    }

    [Fact]
    public void Issue_statement_updates_active_snapshot_and_unbilled_balance()
    {
        var account = OpenDefault();

        account.IssueStatement(
            new CreditCardStatementId("stmt-explicit"),
            new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"),
            DateTimeOffset.Parse("2026-05-16T00:00:00+00:00"));

        Assert.Equal(7000m, account.ActiveStatementBalance);
        Assert.Equal(7000m, account.ActiveStatementOutstandingBalance);
        Assert.Equal(3458m, account.UnbilledBalance);
        var issued = Assert.IsType<CreditCardStatementIssued>(Assert.Single(account.GetUncommittedEvents().OfType<CreditCardStatementIssued>()));
        Assert.Equal("stmt-explicit", issued.StatementId);
    }

    [Fact]
    public void Open_uses_provided_timestamp_for_domain_event()
    {
        var openedAt = DateTimeOffset.Parse("2026-05-20T10:15:00+00:00");

        var account = CreditCardAccount.Open(
            new CreditCardAccountId("card-1"),
            new UserId("user-1"),
            "mBank Visa",
            Currency.PLN,
            new FundingAccountId("fund-1"),
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532",
            openedAt);

        var opened = Assert.IsType<CreditCardAccountOpened>(Assert.Single(account.GetUncommittedEvents().OfType<CreditCardAccountOpened>()));
        Assert.Equal(openedAt, opened.Timestamp);
    }

    [Fact]
    public void Seed_active_statement_records_explicit_seed_statement_id()
    {
        var account = OpenDefault();

        account.SeedState(
            new CreditCardStatementId("stmt-seeded"),
            100m,
            5m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            25m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"));

        var seeded = Assert.IsType<CreditCardStateSeeded>(Assert.Single(account.GetUncommittedEvents().OfType<CreditCardStateSeeded>()));
        Assert.Equal("stmt-seeded", seeded.ActiveStatementId);
        Assert.Equal("stmt-seeded", Assert.Single(account.GetOpenStatementSnapshots()).StatementId);
    }

    [Fact]
    public void Replay_old_seed_event_without_statement_id_uses_legacy_seed_statement_id()
    {
        var account = OpenDefault();
        account.ReplayEvents([
            new CreditCardStateSeeded(
                "card-1",
                null,
                100m,
                5m,
                new DateOnly(2026, 5, 16),
                new DateOnly(2026, 6, 9),
                25m,
                DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"))
        ]);

        Assert.Equal("stmt-1", Assert.Single(account.GetOpenStatementSnapshots()).StatementId);
    }

    [Fact]
    public void Apply_settlement_transfer_reduces_outstanding_balance_without_touching_unbilled_balance()
    {
        var account = OpenDefault();

        account.SeedState(
            0m,
            0m,
            null,
            null,
            10458m);

        account.IssueStatement(
            new CreditCardStatementId("stmt-1"),
            new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"),
            CreditCardAccountTestExtensions.TestOccurredAtUtc);

        account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            7000m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 7000m)]);

        Assert.Equal(0m, account.ActiveStatementOutstandingBalance);
        Assert.Equal(3458m, account.UnbilledBalance);
        Assert.Contains(account.GetUncommittedEvents(), x => x is CreditCardStatementPaymentApplied);
    }

    [Fact]
    public void Replay_after_issue_and_payment_restores_outstanding_balance_and_unbilled_balance()
    {
        var original = OpenDefault();
        original.SeedState(0m, 0m, null, null, 10458m);
        IssueDefaultStatement(original);
        original.ApplySettlementTransfer(
            new TransferId("trf-1"),
            7000m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 7000m)]);

        var replayed = new CreditCardAccount();
        replayed.ReplayEvents(original.GetUncommittedEvents());

        Assert.Equal(0m, replayed.ActiveStatementOutstandingBalance);
        Assert.Equal(7000m, replayed.ActiveStatementBalance);
        Assert.Equal(3458m, replayed.UnbilledBalance);
        Assert.Empty(replayed.GetOpenStatementSnapshots());
    }

    [Fact]
    public void Apply_same_transfer_to_same_statement_is_idempotent()
    {
        var account = OpenDefault();
        IssueDefaultStatement(account);
        account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            100m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 100m)]);
        account.ClearUncommittedEvents();

        account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            100m,
            DateTimeOffset.Parse("2026-05-20T10:16:00+00:00"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 100m)]);

        Assert.Equal(6900m, account.ActiveStatementOutstandingBalance);
        Assert.Empty(account.GetUncommittedEvents());
    }

    [Fact]
    public void Reusing_transfer_with_different_allocations_is_rejected()
    {
        var account = OpenDefault();
        account.IssueStatement(
            new CreditCardStatementId("stmt-1"),
            new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"),
            CreditCardAccountTestExtensions.TestOccurredAtUtc);
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 5, 17),
            PeriodTo: new DateOnly(2026, 6, 16),
            StatementDate: new DateOnly(2026, 6, 16),
            DueDate: new DateOnly(2026, 7, 10),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            50m,
            DateTimeOffset.Parse("2026-06-20T10:15:00+00:00"),
            [new CreditCardPaymentAllocationDecision("stmt-1", 50m)]);
        account.ClearUncommittedEvents();

        var beforeSnapshots = account.GetOpenStatementSnapshots();

        var ex = Assert.Throws<SettlementTransferAlreadyAppliedWithDifferentAllocationsException>(() => account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            100m,
            DateTimeOffset.Parse("2026-06-20T10:16:00+00:00"),
            [
                new CreditCardPaymentAllocationDecision("stmt-1", 50m),
                new CreditCardPaymentAllocationDecision("stmt-2", 50m)
            ]));

        Assert.Equal("Settlement transfer already applied with different allocations.", ex.Message);
        Assert.Equal(beforeSnapshots, account.GetOpenStatementSnapshots());
        Assert.Empty(account.GetUncommittedEvents());
    }

    [Fact]
    public void Reissued_statement_for_same_period_is_idempotent()
    {
        var account = OpenDefault();
        IssueDefaultStatement(account);
        account.ClearUncommittedEvents();

        IssueDefaultStatement(account);

        Assert.Empty(account.GetUncommittedEvents());
        Assert.Single(account.GetOpenStatementSnapshots());
        Assert.Equal(7000m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Reissued_statement_for_same_period_with_different_values_is_rejected()
    {
        var account = OpenDefault();
        IssueDefaultStatement(account);
        account.ClearUncommittedEvents();

        var ex = Assert.Throws<StatementAlreadyIssuedWithDifferentValuesException>(() => CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 10),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04")));

        Assert.Equal("Statement already issued with different values.", ex.Message);
        Assert.Empty(account.GetUncommittedEvents());
        Assert.Equal(7000m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Repeated_seed_state_with_same_snapshot_is_idempotent()
    {
        var account = OpenDefault();
        account.SeedState(
            100m,
            5m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            25m);
        account.ClearUncommittedEvents();

        account.SeedState(
            100m,
            5m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            25m);

        Assert.Empty(account.GetUncommittedEvents());
        Assert.Single(account.GetOpenStatementSnapshots());
        Assert.Equal(100m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Different_seed_after_statement_history_is_rejected()
    {
        var account = OpenDefault();
        account.SeedState(
            100m,
            5m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            25m);
        account.ClearUncommittedEvents();

        var ex = Assert.Throws<CannotSeedCreditCardStateAfterStatementHistoryExistsException>(() => account.SeedState(
            0m,
            0m,
            null,
            null,
            25m));

        Assert.Equal("Cannot seed credit card state after statement history exists.", ex.Message);
        Assert.Empty(account.GetUncommittedEvents());
        Assert.Single(account.GetOpenStatementSnapshots());
        Assert.Equal(100m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Invalid_multi_decision_settlement_does_not_partially_mutate_or_apply_events()
    {
        var account = OpenDefault();
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 5, 17),
            PeriodTo: new DateOnly(2026, 6, 16),
            StatementDate: new DateOnly(2026, 6, 16),
            DueDate: new DateOnly(2026, 7, 10),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        account.ClearUncommittedEvents();

        var beforeSnapshots = account.GetOpenStatementSnapshots();

        var ex = Assert.Throws<PaymentApplicationAmountCannotExceedStatementOutstandingBalanceException>(() => account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            150m,
            DateTimeOffset.Parse("2026-07-01T10:15:00+00:00"),
            [
                new CreditCardPaymentAllocationDecision("stmt-1", 50m),
                new CreditCardPaymentAllocationDecision("stmt-2", 150m)
            ]));

        Assert.Equal("Payment application amount cannot exceed statement outstanding balance.", ex.Message);
        Assert.Equal(beforeSnapshots, account.GetOpenStatementSnapshots());
        Assert.Empty(account.GetUncommittedEvents());
        Assert.Equal(100m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Settlement_decisions_exceeding_transfer_amount_do_not_mutate_or_apply_events()
    {
        var account = OpenDefault();
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 5, 17),
            PeriodTo: new DateOnly(2026, 6, 16),
            StatementDate: new DateOnly(2026, 6, 16),
            DueDate: new DateOnly(2026, 7, 10),
            StatementBalance: 100m,
            MinimumPaymentDue: 5m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
        account.ClearUncommittedEvents();

        var beforeSnapshots = account.GetOpenStatementSnapshots();

        var ex = Assert.Throws<PaymentApplicationDecisionsCannotExceedSettlementAmountException>(() => account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            100m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"),
            [
                new CreditCardPaymentAllocationDecision("stmt-1", 80m),
                new CreditCardPaymentAllocationDecision("stmt-2", 40m)
            ]));

        Assert.Equal("Payment application decisions cannot exceed settlement amount.", ex.Message);
        Assert.Equal(beforeSnapshots, account.GetOpenStatementSnapshots());
        Assert.Empty(account.GetUncommittedEvents());
        Assert.Equal(100m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Duplicate_statement_decisions_in_single_settlement_do_not_mutate_or_apply_events()
    {
        var account = OpenDefault();
        IssueDefaultStatement(account);
        account.ClearUncommittedEvents();

        var beforeSnapshots = account.GetOpenStatementSnapshots();

        var ex = Assert.Throws<PaymentApplicationDecisionsMustBeUniquePerStatementException>(() => account.ApplySettlementTransfer(
            new TransferId("trf-1"),
            120m,
            DateTimeOffset.Parse("2026-05-20T10:15:00+00:00"),
            [
                new CreditCardPaymentAllocationDecision("stmt-1", 80m),
                new CreditCardPaymentAllocationDecision("stmt-1", 40m)
            ]));

        Assert.Equal("Payment application decisions must be unique per statement.", ex.Message);
        Assert.Equal(beforeSnapshots, account.GetOpenStatementSnapshots());
        Assert.Empty(account.GetUncommittedEvents());
        Assert.Equal(7000m, account.ActiveStatementOutstandingBalance);
    }

    [Fact]
    public void Issue_statement_rejects_minimum_payment_greater_than_statement_balance()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<MinimumPaymentDueCannotExceedStatementBalanceException>(() => CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 100m,
            MinimumPaymentDue: 101m,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04")));

        Assert.Equal("Minimum payment due cannot exceed statement balance.", ex.Message);
        Assert.DoesNotContain(account.GetUncommittedEvents(), x => x is CreditCardStatementIssued);
    }

    [Fact]
    public void Allocation_policy_orders_by_due_date_and_does_not_exceed_open_balances()
    {
        var policy = new OldestStatementFirstCreditCardPaymentAllocationPolicy();

        var decisions = policy.Allocate(
            120m,
            [
                new OpenStatementSnapshot("stmt-late", new DateOnly(2026, 7, 10), 100m),
                new OpenStatementSnapshot("stmt-early", new DateOnly(2026, 6, 9), 80m)
            ]).ToArray();

        Assert.Equal(2, decisions.Length);
        Assert.Equal(new CreditCardPaymentAllocationDecision("stmt-early", 80m), decisions[0]);
        Assert.Equal(new CreditCardPaymentAllocationDecision("stmt-late", 40m), decisions[1]);
        Assert.All(decisions, decision => Assert.True(decision.Amount <= 100m));
        Assert.Equal(120m, decisions.Sum(x => x.Amount));
    }

    private static CreditCardAccount OpenDefault() =>
        CreditCardAccount.Open(
            new CreditCardAccountId("card-1"),
            new UserId("user-1"),
            "mBank Visa",
            Currency.PLN,
            new FundingAccountId("fund-1"),
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532",
            CreditCardAccountTestExtensions.TestOccurredAtUtc);

    private static void IssueDefaultStatement(CreditCardAccount account)
    {
        CreditCardAccountTestExtensions.IssueStatement(account, new CreditCardStatementComputation(
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04"));
    }
}

internal static class CreditCardAccountTestExtensions
{
    public static DateTimeOffset TestOccurredAtUtc { get; } =
        DateTimeOffset.Parse("2026-05-20T10:15:00+00:00");

    public static void SeedState(
        this CreditCardAccount account,
        decimal activeStatementBalance,
        decimal activeStatementMinimumPaymentDue,
        DateOnly? activeStatementPeriodCloseDate,
        DateOnly? activeStatementDueDate,
        decimal unbilledBalance)
    {
        var statementId = activeStatementBalance > 0m
            ? new CreditCardStatementId($"stmt-seeded-{account.GetOpenStatementSnapshots().Count + 1}")
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

    public static void IssueStatement(
        this CreditCardAccount account,
        CreditCardStatementComputation computation)
    {
        account.IssueStatement(
            new CreditCardStatementId($"stmt-{account.GetOpenStatementSnapshots().Count + 1}"),
            computation,
            TestOccurredAtUtc);
    }
}

using System.Reflection;
using WiSave.Expenses.Contracts.Commands.CreditCards;
using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events.CreditCards;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Tests;

public class CreditCardRedesignContractTests
{
    [Fact]
    public void Open_credit_card_command_exposes_policy_and_settlement_metadata()
    {
        var command = new OpenCreditCardAccount(
            CorrelationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name: "mBank Visa",
            Currency: Currency.PLN,
            SettlementAccountId: "fund-1",
            BankProvider: BankProvider.MBank,
            ProductCode: "STANDARD",
            CreditLimit: 12000m,
            StatementClosingDay: 16,
            GracePeriodDays: 24,
            Color: "#f59e0b",
            LastFourDigits: "4532");

        Assert.Equal("fund-1", command.SettlementAccountId);
        Assert.Equal(BankProvider.MBank, command.BankProvider);
        Assert.Equal("STANDARD", command.ProductCode);
        Assert.Equal(24, command.GracePeriodDays);
    }

    [Fact]
    public void Statement_issued_event_carries_snapshot_fields()
    {
        var @event = new CreditCardStatementIssued(
            CreditCardAccountId: "card-1",
            StatementId: "stmt-1",
            PeriodFrom: new DateOnly(2026, 4, 17),
            PeriodTo: new DateOnly(2026, 5, 16),
            StatementDate: new DateOnly(2026, 5, 16),
            DueDate: new DateOnly(2026, 6, 9),
            StatementBalance: 7000m,
            MinimumPaymentDue: 350m,
            UnbilledBalanceAfterIssue: 3458m,
            PolicyCode: "MBANK_STANDARD",
            PolicyVersion: "2026-04",
            Timestamp: DateTimeOffset.UtcNow);

        Assert.Equal("stmt-1", @event.StatementId);
        Assert.Equal(7000m, @event.StatementBalance);
        Assert.Equal(3458m, @event.UnbilledBalanceAfterIssue);
    }

    [Fact]
    public void Issue_statement_command_exposes_calculation_date()
    {
        var command = new IssueCreditCardStatement(
            Guid.NewGuid(),
            "user-1",
            "card-1",
            new DateOnly(2026, 5, 16));

        Assert.Equal("user-1", command.UserId);
        Assert.Equal("card-1", command.CreditCardAccountId);
        Assert.Equal(new DateOnly(2026, 5, 16), command.CalculationDate);
    }

    [Fact]
    public void Funding_transfer_contracts_expose_optional_credit_card_settlement_target()
    {
        var command = new PostFundingTransfer(
            CorrelationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId: "user-1",
            FundingAccountId: "fund-1",
            TransferId: "transfer-1",
            TargetCreditCardAccountId: null,
            StatementId: null,
            Amount: 250m,
            PostedAtUtc: DateTimeOffset.Parse("2026-05-16T10:00:00Z"));

        var @event = new FundingTransferPosted(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            TransferId: "transfer-1",
            TargetCreditCardAccountId: null,
            StatementId: null,
            Amount: 250m,
            PostedAtUtc: DateTimeOffset.Parse("2026-05-16T10:00:00Z"),
            Timestamp: DateTimeOffset.Parse("2026-05-16T10:00:01Z"));

        Assert.Null(command.TargetCreditCardAccountId);
        Assert.Null(@event.TargetCreditCardAccountId);
        Assert.Equal("transfer-1", @event.TransferId);
        Assert.Equal(250m, @event.Amount);
        Assert.Equal(DateTimeOffset.Parse("2026-05-16T10:00:01Z"), @event.Timestamp);
    }

    [Fact]
    public void New_id_records_and_enums_expose_expected_values()
    {
        var fundingAccountId = new FundingAccountId("fund-1");
        var creditCardAccountId = new CreditCardAccountId("card-1");
        var statementId = new CreditCardStatementId("stmt-1");
        var transferId = new TransferId("transfer-1");

        Assert.Equal("fund-1", (string)fundingAccountId);
        Assert.Equal("card-1", (string)creditCardAccountId);
        Assert.Equal("stmt-1", (string)statementId);
        Assert.Equal("transfer-1", (string)transferId);
        Assert.Equal(BankProvider.MBank, Enum.Parse<BankProvider>("MBank"));
        Assert.Equal(FundingAccountKind.BankAccount, Enum.Parse<FundingAccountKind>("BankAccount"));
    }

    [Fact]
    public void Funding_account_commands_and_events_expose_expected_shapes()
    {
        var open = new OpenFundingAccount(
            Guid.NewGuid(),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6");
        var update = new UpdateFundingAccount(
            Guid.NewGuid(),
            "user-1",
            "fund-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            "#3b82f6");
        var close = new CloseFundingAccount(Guid.NewGuid(), "user-1", "fund-1");
        var opened = new FundingAccountOpened(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            1500m,
            "#3b82f6",
            DateTimeOffset.UtcNow);
        var updated = new FundingAccountUpdated(
            "fund-1",
            "user-1",
            "Main checking",
            FundingAccountKind.BankAccount,
            Currency.PLN,
            "#3b82f6",
            DateTimeOffset.UtcNow);
        var closed = new FundingAccountClosed("fund-1", "user-1", DateTimeOffset.UtcNow);

        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), open.UserId);
        Assert.Equal("user-1", update.UserId);
        Assert.Equal("fund-1", close.FundingAccountId);
        Assert.Equal(1500m, opened.OpeningBalance);
        Assert.Equal(FundingAccountKind.BankAccount, updated.Kind);
        Assert.Equal("user-1", closed.UserId);
    }

    [Fact]
    public void Credit_card_lifecycle_commands_and_events_expose_expected_shapes()
    {
        var update = new UpdateCreditCardAccount(
            Guid.NewGuid(),
            "user-1",
            "card-1",
            "mBank Visa",
            Currency.PLN,
            "fund-1",
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532");
        var close = new CloseCreditCardAccount(Guid.NewGuid(), "user-1", "card-1");
        var seed = new SeedCreditCardState(
            Guid.NewGuid(),
            "user-1",
            "card-1",
            7000m,
            350m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            3458m);
        var opened = new CreditCardAccountOpened(
            "card-1",
            "user-1",
            "mBank Visa",
            Currency.PLN,
            "fund-1",
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532",
            DateTimeOffset.UtcNow);
        var updated = new CreditCardAccountUpdated(
            "card-1",
            "user-1",
            "mBank Visa",
            Currency.PLN,
            "fund-1",
            BankProvider.MBank,
            "STANDARD",
            12000m,
            16,
            24,
            "#f59e0b",
            "4532",
            DateTimeOffset.UtcNow);
        var stateSeeded = new CreditCardStateSeeded(
            "card-1",
            "stmt-1",
            7000m,
            350m,
            new DateOnly(2026, 5, 16),
            new DateOnly(2026, 6, 9),
            3458m,
            DateTimeOffset.UtcNow);
        var paymentApplied = new CreditCardStatementPaymentApplied(
            "card-1",
            "stmt-1",
            "transfer-1",
            7000m,
            0m,
            DateTimeOffset.Parse("2026-05-20T10:15:00Z"),
            DateTimeOffset.Parse("2026-05-20T10:15:01Z"));
        var closed = new CreditCardAccountClosed("card-1", "user-1", DateTimeOffset.UtcNow);

        Assert.Equal("user-1", update.UserId);
        Assert.Equal("card-1", close.CreditCardAccountId);
        Assert.Equal(3458m, seed.UnbilledBalance);
        Assert.Equal("fund-1", opened.SettlementAccountId);
        Assert.Equal(BankProvider.MBank, updated.BankProvider);
        Assert.Equal(new DateOnly(2026, 6, 9), stateSeeded.ActiveStatementDueDate);
        Assert.Equal(0m, paymentApplied.StatementOutstandingBalanceAfterApplication);
        Assert.Equal(DateTimeOffset.Parse("2026-05-20T10:15:01Z"), paymentApplied.Timestamp);
        Assert.Equal("user-1", closed.UserId);
    }

    [Fact]
    public void Settlement_target_id_is_nullable_in_transfer_contracts()
    {
        var nullability = new NullabilityInfoContext();

        var commandParameter = typeof(PostFundingTransfer).GetConstructors().Single()
            .GetParameters().Single(x => x.Name == "TargetCreditCardAccountId");
        var eventParameter = typeof(FundingTransferPosted).GetConstructors().Single()
            .GetParameters().Single(x => x.Name == "TargetCreditCardAccountId");

        Assert.Equal(NullabilityState.Nullable, nullability.Create(commandParameter).WriteState);
        Assert.Equal(NullabilityState.Nullable, nullability.Create(eventParameter).WriteState);
    }

    [Fact]
    public void Seed_credit_card_state_allows_no_active_statement_dates()
    {
        var command = new SeedCreditCardState(
            Guid.NewGuid(),
            "user-1",
            "card-1",
            0m,
            0m,
            null,
            null,
            10458m);
        var @event = new CreditCardStateSeeded(
            "card-1",
            null,
            0m,
            0m,
            null,
            null,
            10458m,
            DateTimeOffset.UtcNow);

        Assert.Null(command.ActiveStatementPeriodCloseDate);
        Assert.Null(command.ActiveStatementDueDate);
        Assert.Null(@event.ActiveStatementPeriodCloseDate);
        Assert.Null(@event.ActiveStatementDueDate);
    }
}

using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Funding;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Tests.Funding;

public class FundingAccountTests
{
    private static readonly FundingAccountId Id = new("funding-1");
    private static readonly UserId User = new("user-1");

    [Fact]
    public void Given_valid_details_When_opening_funding_account_Then_opened_event_sets_state()
    {
        var account = FundingAccount.Open(
            Id,
            User,
            "Main cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            120.50m,
            "#16a34a");

        Assert.Equal(Id, account.Id);
        Assert.Equal(User, account.UserId);
        Assert.Equal("Main cash", account.Name);
        Assert.Equal(FundingAccountKind.Cash, account.Kind);
        Assert.Equal(Currency.PLN, account.Currency);
        Assert.Equal(120.50m, account.Balance);
        Assert.Equal("#16a34a", account.Color);
        Assert.True(account.IsActive);

        var @event = Assert.IsType<FundingAccountOpened>(Assert.Single(account.GetUncommittedEvents()));
        Assert.Equal(Id.Value, @event.FundingAccountId);
        Assert.Equal(User.Value, @event.UserId);
        Assert.Equal(120.50m, @event.OpeningBalance);
    }

    [Fact]
    public void Given_funding_account_id_When_building_stream_id_Then_funding_account_stream_prefix_is_used()
    {
        Assert.Equal("funding-account-funding-1", FundingAccount.ToStreamId(Id));
    }

    [Fact]
    public void Given_open_funding_account_When_reconfiguring_Then_updated_event_sets_state()
    {
        var account = OpenDefault();
        account.ClearUncommittedEvents();

        account.Reconfigure("Savings", FundingAccountKind.BankAccount, Currency.EUR, "#2563eb");

        Assert.Equal("Savings", account.Name);
        Assert.Equal(FundingAccountKind.BankAccount, account.Kind);
        Assert.Equal(Currency.EUR, account.Currency);
        Assert.Equal("#2563eb", account.Color);
        Assert.IsType<FundingAccountUpdated>(Assert.Single(account.GetUncommittedEvents()));
    }

    [Fact]
    public void Given_open_funding_account_When_closing_Then_closed_event_marks_inactive()
    {
        var account = OpenDefault();
        account.ClearUncommittedEvents();

        account.Close();

        Assert.False(account.IsActive);
        Assert.IsType<FundingAccountClosed>(Assert.Single(account.GetUncommittedEvents()));
    }

    [Fact]
    public void Given_open_funding_account_When_adding_payment_instrument_Then_child_entity_is_recorded()
    {
        var account = OpenDefault();
        account.ClearUncommittedEvents();

        account.AddPaymentInstrument(
            new PaymentInstrumentId("pi-1"),
            PaymentInstrumentKind.DebitCard,
            "mBank debit",
            "4532",
            "Visa",
            "#0f766e");

        var instrument = Assert.Single(account.PaymentInstruments);
        Assert.Equal("pi-1", instrument.Id.Value);
        Assert.Equal(PaymentInstrumentKind.DebitCard, instrument.Kind);
        Assert.Equal("mBank debit", instrument.Name);
        Assert.True(instrument.IsActive);
        Assert.IsType<FundingPaymentInstrumentAdded>(Assert.Single(account.GetUncommittedEvents()));
    }

    [Fact]
    public void Given_payment_instrument_When_updating_Then_metadata_changes()
    {
        var account = OpenDefault();
        account.AddPaymentInstrument(new PaymentInstrumentId("pi-1"), PaymentInstrumentKind.DebitCard, "mBank debit", "4532", "Visa", "#0f766e");
        account.ClearUncommittedEvents();

        account.UpdatePaymentInstrument(
            new PaymentInstrumentId("pi-1"),
            PaymentInstrumentKind.VirtualDebitCard,
            "mBank virtual",
            "0987",
            "Mastercard",
            "#1d4ed8");

        var instrument = Assert.Single(account.PaymentInstruments);
        Assert.Equal(PaymentInstrumentKind.VirtualDebitCard, instrument.Kind);
        Assert.Equal("mBank virtual", instrument.Name);
        Assert.Equal("0987", instrument.LastFourDigits);
        Assert.IsType<FundingPaymentInstrumentUpdated>(Assert.Single(account.GetUncommittedEvents()));
    }

    [Fact]
    public void Given_payment_instrument_When_removing_Then_child_entity_is_marked_inactive()
    {
        var account = OpenDefault();
        account.AddPaymentInstrument(new PaymentInstrumentId("pi-1"), PaymentInstrumentKind.DebitCard, "mBank debit", "4532", "Visa", "#0f766e");
        account.ClearUncommittedEvents();

        account.RemovePaymentInstrument(new PaymentInstrumentId("pi-1"));

        Assert.False(Assert.Single(account.PaymentInstruments).IsActive);
        Assert.IsType<FundingPaymentInstrumentRemoved>(Assert.Single(account.GetUncommittedEvents()));
    }

    [Fact]
    public void Given_open_funding_account_When_posting_transfer_Then_balance_is_reduced_and_event_is_recorded()
    {
        var account = OpenDefault();
        account.ClearUncommittedEvents();
        var postedAt = DateTimeOffset.Parse("2026-05-16T10:00:00Z");

        account.PostTransfer(
            new TransferId("transfer-1"),
            25m,
            postedAt,
            new CreditCardAccountId("card-1"),
            new CreditCardStatementId("stmt-1"));

        Assert.Equal(75m, account.Balance);
        var @event = Assert.IsType<FundingTransferPosted>(Assert.Single(account.GetUncommittedEvents()));
        Assert.Equal("transfer-1", @event.TransferId);
        Assert.Equal("card-1", @event.TargetCreditCardAccountId);
        Assert.Equal("stmt-1", @event.StatementId);
        Assert.Equal(25m, @event.Amount);
        Assert.Equal(postedAt, @event.PostedAtUtc);
    }

    [Fact]
    public void Given_blank_name_When_opening_Then_domain_exception_is_thrown()
    {
        var ex = Assert.Throws<DomainException>(() => FundingAccount.Open(
            Id,
            User,
            " ",
            FundingAccountKind.Cash,
            Currency.PLN,
            0m,
            null));

        Assert.Equal("Funding account name is required.", ex.Message);
    }

    [Fact]
    public void Given_negative_opening_balance_When_opening_Then_domain_exception_is_thrown()
    {
        var ex = Assert.Throws<DomainException>(() => FundingAccount.Open(
            Id,
            User,
            "Cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            -0.01m,
            null));

        Assert.Equal("Opening balance cannot be negative.", ex.Message);
    }

    [Fact]
    public void Given_closed_funding_account_When_reconfiguring_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();
        account.Close();

        var ex = Assert.Throws<DomainException>(() => account.Reconfigure(
            "Savings",
            FundingAccountKind.BankAccount,
            Currency.EUR,
            null));

        Assert.Equal("Cannot modify a closed funding account.", ex.Message);
    }

    [Fact]
    public void Given_blank_name_When_reconfiguring_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<DomainException>(() => account.Reconfigure(
            " ",
            FundingAccountKind.BankAccount,
            Currency.EUR,
            null));

        Assert.Equal("Funding account name is required.", ex.Message);
    }

    [Fact]
    public void Given_closed_funding_account_When_closing_again_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();
        account.Close();

        var ex = Assert.Throws<DomainException>(account.Close);

        Assert.Equal("Cannot modify a closed funding account.", ex.Message);
    }

    [Fact]
    public void Given_funding_account_events_When_replaying_Then_state_is_restored()
    {
        var original = OpenDefault();
        original.Reconfigure("Savings", FundingAccountKind.BankAccount, Currency.EUR, "#2563eb");
        original.Close();

        var replayed = new FundingAccount();
        replayed.ReplayEvents(original.GetUncommittedEvents());

        Assert.Equal(Id, replayed.Id);
        Assert.Equal(User, replayed.UserId);
        Assert.Equal("Savings", replayed.Name);
        Assert.Equal(FundingAccountKind.BankAccount, replayed.Kind);
        Assert.Equal(Currency.EUR, replayed.Currency);
        Assert.Equal(100m, replayed.Balance);
        Assert.Equal("#2563eb", replayed.Color);
        Assert.False(replayed.IsActive);
    }

    [Fact]
    public void Given_invalid_last_four_digits_When_adding_payment_instrument_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<DomainException>(() => account.AddPaymentInstrument(
            new PaymentInstrumentId("pi-1"),
            PaymentInstrumentKind.DebitCard,
            "mBank debit",
            "45x2",
            "Visa",
            null));

        Assert.Equal("Payment instrument last four digits must contain exactly four digits.", ex.Message);
    }

    [Fact]
    public void Given_non_positive_transfer_amount_When_posting_transfer_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<DomainException>(() => account.PostTransfer(
            new TransferId("transfer-1"),
            0m,
            DateTimeOffset.UtcNow,
            null,
            null));

        Assert.Equal("Funding transfer amount must be greater than zero.", ex.Message);
    }

    [Fact]
    public void Given_transfer_amount_exceeds_balance_When_posting_transfer_Then_domain_exception_is_thrown()
    {
        var account = OpenDefault();

        var ex = Assert.Throws<DomainException>(() => account.PostTransfer(
            new TransferId("transfer-1"),
            100.01m,
            DateTimeOffset.UtcNow,
            null,
            null));

        Assert.Equal("Funding transfer amount cannot exceed account balance.", ex.Message);
    }

    private static FundingAccount OpenDefault() =>
        FundingAccount.Open(
            Id,
            User,
            "Cash",
            FundingAccountKind.Cash,
            Currency.PLN,
            100m,
            null);
}

using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Tests;

public class FundingPaymentInstrumentContractTests
{
    [Fact]
    public void Add_payment_instrument_command_carries_funding_account_metadata()
    {
        var command = new AddFundingPaymentInstrument(
            CorrelationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserId: "user-1",
            FundingAccountId: "fund-1",
            Name: "mBank debit",
            Kind: PaymentInstrumentKind.DebitCard,
            LastFourDigits: "4532",
            Network: "Visa",
            Color: "#0f766e");

        Assert.Equal("fund-1", command.FundingAccountId);
        Assert.Equal(PaymentInstrumentKind.DebitCard, command.Kind);
        Assert.Equal("4532", command.LastFourDigits);
    }

    [Fact]
    public void Payment_instrument_event_carries_child_entity_identity()
    {
        var @event = new FundingPaymentInstrumentAdded(
            FundingAccountId: "fund-1",
            UserId: "user-1",
            PaymentInstrumentId: "pi-1",
            Name: "mBank debit",
            Kind: PaymentInstrumentKind.DebitCard,
            LastFourDigits: "4532",
            Network: "Visa",
            Color: "#0f766e",
            Timestamp: DateTimeOffset.UtcNow);

        Assert.Equal("pi-1", @event.PaymentInstrumentId);
        Assert.Equal(PaymentInstrumentKind.DebitCard, @event.Kind);
    }

    [Fact]
    public void Payment_instrument_id_converts_to_and_from_string()
    {
        var id = new PaymentInstrumentId("pi-1");

        Assert.Equal("pi-1", (string)id);
        Assert.Equal(id, (PaymentInstrumentId)"pi-1");
    }
}

using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.WebApi.Requests.FundingAccounts;

namespace WiSave.Expenses.WebApi.Tests.Requests;

public class FundingAccountRequestMappingTests
{
    [Fact]
    public void Open_request_maps_to_open_funding_account_command()
    {
        var request = new OpenFundingAccountRequest(
            Name: "Main checking",
            Kind: FundingAccountKind.BankAccount,
            Currency: Currency.PLN,
            OpeningBalance: 1500m,
            Color: "#3b82f6");

        var command = request.ToCommand(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), command.CorrelationId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), command.UserId);
        Assert.Equal("Main checking", command.Name);
        Assert.Equal(FundingAccountKind.BankAccount, command.Kind);
        Assert.Equal(Currency.PLN, command.Currency);
        Assert.Equal(1500m, command.OpeningBalance);
        Assert.Equal("#3b82f6", command.Color);
    }

    [Fact]
    public void Update_request_maps_to_update_funding_account_command()
    {
        var request = new UpdateFundingAccountRequest(
            Name: "Emergency cash",
            Kind: FundingAccountKind.Cash,
            Currency: Currency.EUR,
            Color: "#10b981");

        var command = request.ToCommand(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "user-1",
            "fund-1");

        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), command.CorrelationId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("fund-1", command.FundingAccountId);
        Assert.Equal("Emergency cash", command.Name);
        Assert.Equal(FundingAccountKind.Cash, command.Kind);
        Assert.Equal(Currency.EUR, command.Currency);
        Assert.Equal("#10b981", command.Color);
    }

    [Fact]
    public void Add_payment_instrument_request_maps_to_command()
    {
        var request = new AddFundingPaymentInstrumentRequest(
            Name: "mBank debit",
            Kind: PaymentInstrumentKind.DebitCard,
            LastFourDigits: "4532",
            Network: "Visa",
            Color: "#0f766e");

        var command = request.ToCommand(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "user-1",
            "fund-1");

        Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), command.CorrelationId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("fund-1", command.FundingAccountId);
        Assert.Equal(PaymentInstrumentKind.DebitCard, command.Kind);
        Assert.Equal("4532", command.LastFourDigits);
    }

    [Fact]
    public void Post_transfer_request_maps_to_command()
    {
        var request = new PostFundingTransferRequest(
            Amount: 25m,
            PostedAtUtc: DateTimeOffset.Parse("2026-05-16T10:00:00Z"));

        var command = request.ToCommand(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "user-1",
            "fund-1",
            "transfer-1");

        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), command.CorrelationId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("fund-1", command.FundingAccountId);
        Assert.Equal("transfer-1", command.TransferId);
        Assert.Equal(25m, command.Amount);
        Assert.Equal(DateTimeOffset.Parse("2026-05-16T10:00:00Z"), command.PostedAtUtc);
    }
}

using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.WebApi.Requests.CreditCards;

namespace WiSave.Expenses.WebApi.Tests.Requests;

public class CreditCardAccountRequestMappingTests
{
    [Fact]
    public void Open_request_maps_to_open_credit_card_account_command()
    {
        var request = new OpenCreditCardAccountRequest(
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

        var command = request.ToCommand(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), command.CorrelationId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), command.UserId);
        Assert.Equal("mBank Visa", command.Name);
        Assert.Equal(Currency.PLN, command.Currency);
        Assert.Equal("fund-1", command.SettlementAccountId);
        Assert.Equal(BankProvider.MBank, command.BankProvider);
        Assert.Equal("STANDARD", command.ProductCode);
        Assert.Equal(12000m, command.CreditLimit);
        Assert.Equal(16, command.StatementClosingDay);
        Assert.Equal(24, command.GracePeriodDays);
        Assert.Equal("#f59e0b", command.Color);
        Assert.Equal("4532", command.LastFourDigits);
    }

    [Fact]
    public void Update_request_maps_to_update_credit_card_account_command()
    {
        var request = new UpdateCreditCardAccountRequest(
            Name: "mBank Visa Platinum",
            Currency: Currency.EUR,
            SettlementAccountId: "fund-2",
            BankProvider: BankProvider.Other,
            ProductCode: "PLATINUM",
            CreditLimit: 20000m,
            StatementClosingDay: 20,
            GracePeriodDays: 25,
            Color: "#0f766e",
            LastFourDigits: "9999");

        var command = request.ToCommand(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "user-1",
            "card-1");

        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), command.CorrelationId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("card-1", command.CreditCardAccountId);
        Assert.Equal("mBank Visa Platinum", command.Name);
        Assert.Equal(Currency.EUR, command.Currency);
        Assert.Equal("fund-2", command.SettlementAccountId);
        Assert.Equal(BankProvider.Other, command.BankProvider);
        Assert.Equal("PLATINUM", command.ProductCode);
        Assert.Equal(20000m, command.CreditLimit);
        Assert.Equal(20, command.StatementClosingDay);
        Assert.Equal(25, command.GracePeriodDays);
        Assert.Equal("#0f766e", command.Color);
        Assert.Equal("9999", command.LastFourDigits);
    }

    [Fact]
    public void Seed_state_request_maps_to_seed_credit_card_state_command()
    {
        var request = new SeedCreditCardStateRequest(
            ActiveStatementBalance: 7000m,
            ActiveStatementMinimumPaymentDue: 350m,
            ActiveStatementPeriodCloseDate: new DateOnly(2026, 5, 16),
            ActiveStatementDueDate: new DateOnly(2026, 6, 9),
            UnbilledBalance: 3458m);

        var command = request.ToCommand(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "user-1",
            "card-1");

        Assert.Equal("user-1", command.UserId);
        Assert.Equal("card-1", command.CreditCardAccountId);
        Assert.Equal(7000m, command.ActiveStatementBalance);
        Assert.Equal(350m, command.ActiveStatementMinimumPaymentDue);
        Assert.Equal(new DateOnly(2026, 5, 16), command.ActiveStatementPeriodCloseDate);
        Assert.Equal(new DateOnly(2026, 6, 9), command.ActiveStatementDueDate);
        Assert.Equal(3458m, command.UnbilledBalance);
    }

    [Fact]
    public void Seed_state_request_allows_unbilled_only_snapshot()
    {
        var request = new SeedCreditCardStateRequest(
            ActiveStatementBalance: 0m,
            ActiveStatementMinimumPaymentDue: 0m,
            ActiveStatementPeriodCloseDate: null,
            ActiveStatementDueDate: null,
            UnbilledBalance: 10458m);

        var command = request.ToCommand(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "user-1",
            "card-1");

        Assert.Equal(0m, command.ActiveStatementBalance);
        Assert.Equal(0m, command.ActiveStatementMinimumPaymentDue);
        Assert.Null(command.ActiveStatementPeriodCloseDate);
        Assert.Null(command.ActiveStatementDueDate);
        Assert.Equal(10458m, command.UnbilledBalance);
    }

    [Fact]
    public void Issue_statement_request_maps_to_issue_credit_card_statement_command()
    {
        var request = new IssueCreditCardStatementRequest(new DateOnly(2026, 5, 16));

        var command = request.ToCommand(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "user-1",
            "card-1");

        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), command.CorrelationId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("card-1", command.CreditCardAccountId);
        Assert.Equal(new DateOnly(2026, 5, 16), command.CalculationDate);
    }
}

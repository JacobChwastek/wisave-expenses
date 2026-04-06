using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.Tests.CreditCards;

public class MBankStatementPolicyTests
{
    [Fact]
    public void Compute_on_closing_day_moves_current_unbilled_balance_to_statement()
    {
        var policy = new MBankStatementPolicy();

        var computation = policy.Compute(new CreditCardStatementPolicyContext(
            AccountId: new CreditCardAccountId("card-1"),
            Currency: Currency.PLN,
            CreditLimit: 12000m,
            CurrentUnbilledBalance: 10458m,
            StatementClosingDay: new StatementClosingDay(16),
            GracePeriodDays: new GracePeriodDays(24),
            CalculationDate: new DateOnly(2026, 5, 16)));

        Assert.Equal(new DateOnly(2026, 4, 17), computation.PeriodFrom);
        Assert.Equal(new DateOnly(2026, 5, 16), computation.PeriodTo);
        Assert.Equal(new DateOnly(2026, 5, 16), computation.StatementDate);
        Assert.Equal(new DateOnly(2026, 6, 9), computation.DueDate);
        Assert.Equal(10458m, computation.StatementBalance);
        Assert.Equal(522.90m, computation.MinimumPaymentDue);
        Assert.Equal(0m, computation.UnbilledBalanceAfterIssue);
        Assert.Equal("MBANK_STANDARD", computation.PolicyCode);
        Assert.Equal("2026-04", computation.PolicyVersion);
    }
}

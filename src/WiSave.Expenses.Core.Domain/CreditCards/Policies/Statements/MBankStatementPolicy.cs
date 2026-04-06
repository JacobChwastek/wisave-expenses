using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;

/// <summary>
/// mBank standard credit-card statement policy.
/// </summary>
/// <remarks>
/// The policy closes the whole current unbilled balance on the configured
/// statement closing day, computes the minimum payment as five percent of the
/// statement balance, and places the due date after the configured grace period.
/// </remarks>
public sealed class MBankStatementPolicy : ICreditCardStatementPolicy
{
    /// <summary>Stable code persisted with statement events produced by this policy.</summary>
    public const string Code = "MBANK_STANDARD";

    /// <summary>Version of the mBank standard statement rules.</summary>
    public const string Version = "2026-04";

    /// <inheritdoc />
    public CreditCardStatementComputation Compute(CreditCardStatementPolicyContext context)
    {
        if (context.CurrentUnbilledBalance < 0m)
            throw new CurrentUnbilledBalanceCannotBeNegativeException();

        var periodTo = ResolveClosingDate(
            context.CalculationDate.Year,
            context.CalculationDate.Month,
            context.StatementClosingDay.Value);
        if (context.CalculationDate != periodTo)
            throw new StatementCanOnlyBeComputedOnConfiguredClosingDayException();

        var previousMonth = periodTo.AddMonths(-1);
        var previousClose = ResolveClosingDate(
            previousMonth.Year,
            previousMonth.Month,
            context.StatementClosingDay.Value);
        var statementBalance = context.CurrentUnbilledBalance;
        var minimumPaymentDue = Math.Round(statementBalance * 0.05m, 2, MidpointRounding.AwayFromZero);

        return new CreditCardStatementComputation(
            PeriodFrom: previousClose.AddDays(1),
            PeriodTo: periodTo,
            StatementDate: context.CalculationDate,
            DueDate: context.CalculationDate.AddDays(context.GracePeriodDays.Value),
            StatementBalance: statementBalance,
            MinimumPaymentDue: minimumPaymentDue,
            UnbilledBalanceAfterIssue: 0m,
            PolicyCode: Code,
            PolicyVersion: Version);
    }

    private static DateOnly ResolveClosingDate(int year, int month, int closingDay)
    {
        var day = Math.Min(closingDay, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }
}

using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;

public sealed record ActiveStatementState
{
    public static ActiveStatementState None { get; } = new();

    public decimal? Balance { get; }
    public decimal? OutstandingBalance { get; }
    public decimal? MinimumPaymentDue { get; }
    public DateOnly? DueDate { get; }
    public DateOnly? PeriodCloseDate { get; }

    private ActiveStatementState() { }

    private ActiveStatementState(
        decimal balance,
        decimal outstandingBalance,
        decimal minimumPaymentDue,
        DateOnly periodCloseDate,
        DateOnly dueDate)
    {
        if (balance <= 0m)
            throw new ActiveStatementBalanceMustBeGreaterThanZeroException();

        if (outstandingBalance < 0m)
            throw new ActiveStatementOutstandingBalanceCannotBeNegativeException();

        if (outstandingBalance > balance)
            throw new ActiveStatementOutstandingBalanceCannotExceedActiveStatementBalanceException();

        if (minimumPaymentDue < 0m)
            throw new ActiveStatementMinimumPaymentDueCannotBeNegativeException();

        if (minimumPaymentDue > balance)
            throw new ActiveStatementMinimumPaymentDueCannotExceedActiveStatementBalanceException();

        Balance = balance;
        OutstandingBalance = outstandingBalance;
        MinimumPaymentDue = minimumPaymentDue;
        PeriodCloseDate = periodCloseDate;
        DueDate = dueDate;
    }

    public static ActiveStatementState Open(
        decimal balance,
        decimal outstandingBalance,
        decimal minimumPaymentDue,
        DateOnly periodCloseDate,
        DateOnly dueDate) =>
        new(balance, outstandingBalance, minimumPaymentDue, periodCloseDate, dueDate);

    public static ActiveStatementState FromSeed(
        decimal balance,
        decimal minimumPaymentDue,
        DateOnly? periodCloseDate,
        DateOnly? dueDate)
    {
        if (balance < 0m)
            throw new ActiveStatementBalanceCannotBeNegativeException();

        if (minimumPaymentDue < 0m)
            throw new ActiveStatementMinimumPaymentDueCannotBeNegativeException();

        if (balance == 0m)
        {
            if (minimumPaymentDue != 0m || periodCloseDate is not null || dueDate is not null)
                throw new ZeroActiveStatementSeedCannotIncludeMinimumPaymentOrDatesException();

            return None;
        }

        if (periodCloseDate is null || dueDate is null)
            throw new ActiveStatementSeedRequiresPeriodCloseDateAndDueDateException();

        return Open(balance, balance, minimumPaymentDue, periodCloseDate.Value, dueDate.Value);
    }

    public ActiveStatementState WithOutstandingBalance(decimal outstandingBalance)
    {
        if (Balance is null || MinimumPaymentDue is null || PeriodCloseDate is null || DueDate is null)
            return None;

        return Open(Balance.Value, outstandingBalance, MinimumPaymentDue.Value, PeriodCloseDate.Value, DueDate.Value);
    }
}

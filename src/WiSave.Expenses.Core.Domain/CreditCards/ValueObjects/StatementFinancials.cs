using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

namespace WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;

public sealed record StatementFinancials
{
    public decimal StatementBalance { get; private init; }
    public decimal MinimumPaymentDue { get; private init; }
    public decimal UnbilledBalanceAfterIssue { get; private init; }
    public decimal OutstandingBalance { get; private init; }

    public StatementFinancials(
        decimal statementBalance,
        decimal minimumPaymentDue,
        decimal unbilledBalanceAfterIssue,
        decimal outstandingBalance)
    {
        if (statementBalance < 0m)
            throw new StatementBalanceCannotBeNegativeException();

        if (minimumPaymentDue < 0m)
            throw new MinimumPaymentDueCannotBeNegativeException();

        if (minimumPaymentDue > statementBalance)
            throw new MinimumPaymentDueCannotExceedStatementBalanceException();

        if (unbilledBalanceAfterIssue < 0m)
            throw new UnbilledBalanceCannotBeNegativeException();

        if (outstandingBalance < 0m)
            throw new StatementOutstandingBalanceCannotBeNegativeException();

        if (outstandingBalance > statementBalance)
            throw new StatementOutstandingBalanceCannotExceedStatementBalanceException();

        StatementBalance = statementBalance;
        MinimumPaymentDue = minimumPaymentDue;
        UnbilledBalanceAfterIssue = unbilledBalanceAfterIssue;
        OutstandingBalance = outstandingBalance;
    }

    public StatementFinancials ApplyPayment(StatementPaymentAmount amount)
    {
        if (amount.Value > OutstandingBalance)
            throw new PaymentApplicationAmountCannotExceedStatementOutstandingBalanceException();

        return this with { OutstandingBalance = OutstandingBalance - amount.Value };
    }
}

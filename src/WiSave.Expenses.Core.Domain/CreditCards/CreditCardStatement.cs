using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;

namespace WiSave.Expenses.Core.Domain.CreditCards;

public sealed class CreditCardStatement
{
    private readonly List<StatementPaymentApplication> _paymentApplications = [];
    private StatementFinancials _financials;

    public CreditCardStatementId Id { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public DateOnly StatementDate { get; }
    public DateOnly DueDate { get; }
    public decimal StatementBalance => _financials.StatementBalance;
    public decimal MinimumPaymentDue => _financials.MinimumPaymentDue;
    public string PolicyCode { get; }
    public string PolicyVersion { get; }
    public decimal UnbilledBalanceAfterIssue => _financials.UnbilledBalanceAfterIssue;
    public decimal OutstandingBalance => _financials.OutstandingBalance;
    public IReadOnlyCollection<StatementPaymentApplication> PaymentApplications => _paymentApplications.AsReadOnly();

    public CreditCardStatement(
        CreditCardStatementId id,
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly statementDate,
        DateOnly dueDate,
        decimal statementBalance,
        decimal minimumPaymentDue,
        string policyCode,
        string policyVersion,
        decimal unbilledBalanceAfterIssue,
        decimal outstandingBalance)
    {
        Id = id;
        PeriodFrom = periodFrom;
        PeriodTo = periodTo;
        StatementDate = statementDate;
        DueDate = dueDate;
        PolicyCode = policyCode;
        PolicyVersion = policyVersion;
        _financials = new StatementFinancials(
            statementBalance,
            minimumPaymentDue,
            unbilledBalanceAfterIssue,
            outstandingBalance);
    }

    public bool HasPaymentApplication(TransferId transferId) =>
        _paymentApplications.Any(x => x.TransferId == transferId);

    public decimal ApplyPayment(TransferId transferId, decimal amount, DateTimeOffset appliedAtUtc)
    {
        if (HasPaymentApplication(transferId))
            return OutstandingBalance;

        var paymentAmount = new StatementPaymentAmount(amount);

        _financials = _financials.ApplyPayment(paymentAmount);
        _paymentApplications.Add(new StatementPaymentApplication(transferId, paymentAmount, appliedAtUtc));
        return OutstandingBalance;
    }
}

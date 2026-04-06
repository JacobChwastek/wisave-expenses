using WiSave.Expenses.Contracts.Events.CreditCards;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.CreditCards.Exceptions;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Payments;
using WiSave.Expenses.Core.Domain.CreditCards.Policies.Statements;
using WiSave.Expenses.Core.Domain.CreditCards.Specifications;
using WiSave.Expenses.Core.Domain.CreditCards.ValueObjects;
using WiSave.Expenses.Core.Domain.SharedKernel;
using WiSave.Expenses.Core.Domain.SharedKernel.ValueObjects;

namespace WiSave.Expenses.Core.Domain.CreditCards;

public sealed class CreditCardAccount : AggregateRoot<CreditCardAccountId>, IAggregateStream<CreditCardAccountId>
{
    private readonly List<CreditCardStatement> _statements = [];
    private CreditCardStatementId? _activeStatementId;
    private ActiveStatementState _activeStatement = ActiveStatementState.None;

    public UserId UserId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public Currency Currency { get; private set; }
    public FundingAccountId SettlementAccountId { get; private set; } = null!;
    public BankProvider BankProvider { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }
    public StatementClosingDay StatementClosingDay { get; private set; } = null!;
    public GracePeriodDays GracePeriodDays { get; private set; } = null!;
    public decimal UnbilledBalance { get; private set; }
    public decimal? ActiveStatementBalance => _activeStatement.Balance;
    public decimal? ActiveStatementOutstandingBalance => _activeStatement.OutstandingBalance;
    public decimal? ActiveStatementMinimumPaymentDue => _activeStatement.MinimumPaymentDue;
    public DateOnly? ActiveStatementDueDate => _activeStatement.DueDate;
    public DateOnly? ActiveStatementPeriodCloseDate => _activeStatement.PeriodCloseDate;
    public string? Color { get; private set; }
    public string? LastFourDigits { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static string ToStreamId(CreditCardAccountId id) => $"credit-card-account-{id.Value}";

    public CreditCardAccount() { }

    public static CreditCardAccount Open(
        CreditCardAccountId id,
        UserId userId,
        string name,
        Currency currency,
        FundingAccountId settlementAccountId,
        BankProvider bankProvider,
        string productCode,
        decimal creditLimit,
        int statementClosingDay,
        int gracePeriodDays,
        string? color,
        string? lastFourDigits,
        DateTimeOffset occurredAtUtc) =>
        Open(
            id,
            userId,
            name,
            currency,
            settlementAccountId,
            bankProvider,
            productCode,
            creditLimit,
            new StatementClosingDay(statementClosingDay),
            new GracePeriodDays(gracePeriodDays),
            color,
            lastFourDigits,
            occurredAtUtc);

    public static CreditCardAccount Open(
        CreditCardAccountId id,
        UserId userId,
        string name,
        Currency currency,
        FundingAccountId settlementAccountId,
        BankProvider bankProvider,
        string productCode,
        decimal creditLimit,
        StatementClosingDay statementClosingDay,
        GracePeriodDays gracePeriodDays,
        string? color,
        string? lastFourDigits,
        DateTimeOffset occurredAtUtc)
    {
        EnsureValidConfiguration(name, settlementAccountId, creditLimit);

        var account = new CreditCardAccount();
        account.RaiseEvent(new CreditCardAccountOpened(
            id.Value,
            userId.Value,
            name,
            currency,
            settlementAccountId.Value,
            bankProvider,
            productCode,
            creditLimit,
            statementClosingDay.Value,
            gracePeriodDays.Value,
            color,
            lastFourDigits,
            occurredAtUtc));
        return account;
    }

    public void Reconfigure(
        string name,
        Currency currency,
        FundingAccountId settlementAccountId,
        BankProvider bankProvider,
        string productCode,
        decimal creditLimit,
        int statementClosingDay,
        int gracePeriodDays,
        string? color,
        string? lastFourDigits,
        DateTimeOffset occurredAtUtc) =>
        Reconfigure(
            name,
            currency,
            settlementAccountId,
            bankProvider,
            productCode,
            creditLimit,
            new StatementClosingDay(statementClosingDay),
            new GracePeriodDays(gracePeriodDays),
            color,
            lastFourDigits,
            occurredAtUtc);

    public void Reconfigure(
        string name,
        Currency currency,
        FundingAccountId settlementAccountId,
        BankProvider bankProvider,
        string productCode,
        decimal creditLimit,
        StatementClosingDay statementClosingDay,
        GracePeriodDays gracePeriodDays,
        string? color,
        string? lastFourDigits,
        DateTimeOffset occurredAtUtc)
    {
        EnsureActive();
        EnsureValidConfiguration(name, settlementAccountId, creditLimit);

        RaiseEvent(new CreditCardAccountUpdated(
            Id.Value,
            UserId.Value,
            name,
            currency,
            settlementAccountId.Value,
            bankProvider,
            productCode,
            creditLimit,
            statementClosingDay.Value,
            gracePeriodDays.Value,
            color,
            lastFourDigits,
            occurredAtUtc));
    }

    public void Close(DateTimeOffset occurredAtUtc)
    {
        EnsureActive();
        RaiseEvent(new CreditCardAccountClosed(Id.Value, UserId.Value, occurredAtUtc));
    }

    public void SeedState(
        CreditCardStatementId? activeStatementId,
        decimal activeStatementBalance,
        decimal activeStatementMinimumPaymentDue,
        DateOnly? activeStatementPeriodCloseDate,
        DateOnly? activeStatementDueDate,
        decimal unbilledBalance,
        DateTimeOffset occurredAtUtc)
    {
        EnsureActive();

        if (unbilledBalance < 0m)
            throw new UnbilledBalanceCannotBeNegativeException();

        var seededActiveStatement = ActiveStatementState.FromSeed(
            activeStatementBalance,
            activeStatementMinimumPaymentDue,
            activeStatementPeriodCloseDate,
            activeStatementDueDate);

        if (seededActiveStatement != ActiveStatementState.None && activeStatementId is null)
            throw new ActiveStatementSeedRequiresStatementIdException();

        if (IsMatchingSeedState(
                seededActiveStatement,
                unbilledBalance))
            return;

        if (_statements.Count > 0)
            throw new CannotSeedCreditCardStateAfterStatementHistoryExistsException();

        RaiseEvent(new CreditCardStateSeeded(
            Id.Value,
            seededActiveStatement == ActiveStatementState.None ? null : activeStatementId?.Value,
            activeStatementBalance,
            activeStatementMinimumPaymentDue,
            activeStatementPeriodCloseDate,
            activeStatementDueDate,
            unbilledBalance,
            occurredAtUtc));
    }

    public void IssueStatement(CreditCardStatementId statementId, CreditCardStatementComputation computation, DateTimeOffset occurredAtUtc)
    {
        EnsureActive();

        if (computation.StatementBalance < 0m)
            throw new StatementBalanceCannotBeNegativeException();

        if (computation.MinimumPaymentDue < 0m)
            throw new MinimumPaymentDueCannotBeNegativeException();

        if (computation.MinimumPaymentDue > computation.StatementBalance)
            throw new MinimumPaymentDueCannotExceedStatementBalanceException();

        if (computation.UnbilledBalanceAfterIssue < 0m)
            throw new UnbilledBalanceCannotBeNegativeException();

        var existingStatement = _statements.SingleOrDefault(x =>
            x.PeriodFrom == computation.PeriodFrom
            && x.PeriodTo == computation.PeriodTo
            && x.StatementDate == computation.StatementDate);

        if (existingStatement is not null)
        {
            if (!new StatementMatchesComputationSpecification(computation).IsSatisfiedBy(existingStatement))
                throw new StatementAlreadyIssuedWithDifferentValuesException();

            return;
        }

        RaiseEvent(new CreditCardStatementIssued(
            Id.Value,
            statementId.Value,
            computation.PeriodFrom,
            computation.PeriodTo,
            computation.StatementDate,
            computation.DueDate,
            computation.StatementBalance,
            computation.MinimumPaymentDue,
            computation.UnbilledBalanceAfterIssue,
            computation.PolicyCode,
            computation.PolicyVersion,
            occurredAtUtc));
    }

    public IReadOnlyCollection<OpenStatementSnapshot> GetOpenStatementSnapshots() =>
        _statements
            .Where(x => x.OutstandingBalance > 0m)
            .OrderBy(x => x.DueDate)
            .Select(x => new OpenStatementSnapshot(x.Id.Value, x.DueDate, x.OutstandingBalance))
            .ToList();

    public void ApplySettlementTransfer(
        TransferId transferId,
        decimal amount,
        DateTimeOffset appliedAtUtc,
        IReadOnlyCollection<CreditCardPaymentAllocationDecision> decisions)
    {
        EnsureActive();

        if (amount <= 0m)
            throw new SettlementAmountMustBeGreaterThanZeroException();

        var requestedApplications = NormalizeRequestedApplications(decisions);
        var existingApplications = GetExistingApplications(transferId);
        if (existingApplications.Count > 0)
        {
            var matchesExistingApplications = new PaymentApplicationsMatchSpecification(requestedApplications)
                .IsSatisfiedBy(existingApplications);
            if (matchesExistingApplications && requestedApplications.Values.Sum() <= amount)
                return;

            throw new SettlementTransferAlreadyAppliedWithDifferentAllocationsException();
        }

        var applications = new List<(CreditCardPaymentAllocationDecision Decision, CreditCardStatement Statement)>();

        foreach (var (statementId, requestedAmount) in requestedApplications)
        {
            var statement = _statements.SingleOrDefault(x => x.Id.Value == statementId)
                ?? throw new StatementNotFoundException();

            if (requestedAmount > statement.OutstandingBalance)
                throw new PaymentApplicationAmountCannotExceedStatementOutstandingBalanceException();

            applications.Add((new CreditCardPaymentAllocationDecision(statementId, requestedAmount), statement));
        }

        if (requestedApplications.Values.Sum() > amount)
            throw new PaymentApplicationDecisionsCannotExceedSettlementAmountException();

        foreach (var (decision, statement) in applications)
        {
            RaiseEvent(new CreditCardStatementPaymentApplied(
                Id.Value,
                statement.Id.Value,
                transferId.Value,
                decision.Amount,
                statement.OutstandingBalance - decision.Amount,
                appliedAtUtc,
                DateTimeOffset.UtcNow));
        }
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new CannotModifyClosedCreditCardAccountException();
    }

    private static Dictionary<string, decimal> NormalizeRequestedApplications(
        IReadOnlyCollection<CreditCardPaymentAllocationDecision> decisions)
    {
        var applications = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var decision in decisions)
        {
            if (decision.Amount <= 0m)
                throw new PaymentApplicationAmountMustBeGreaterThanZeroException();

            if (!applications.TryAdd(decision.StatementId, decision.Amount))
                throw new PaymentApplicationDecisionsMustBeUniquePerStatementException();
        }

        return applications;
    }

    private Dictionary<string, decimal> GetExistingApplications(TransferId transferId) =>
        _statements
            .SelectMany(statement => statement.PaymentApplications
                .Where(application => application.TransferId == transferId)
                .Select(application => new { StatementId = statement.Id.Value, Amount = application.Amount.Value }))
            .ToDictionary(x => x.StatementId, x => x.Amount, StringComparer.Ordinal);

    private static void EnsureValidConfiguration(
        string name,
        FundingAccountId settlementAccountId,
        decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new CreditCardAccountNameRequiredException();

        if (string.IsNullOrWhiteSpace(settlementAccountId.Value))
            throw new SettlementAccountRequiredException();

        if (creditLimit <= 0m)
            throw new CreditLimitMustBeGreaterThanZeroException();
    }

    private bool IsMatchingSeedState(
        ActiveStatementState seededActiveStatement,
        decimal unbilledBalance)
    {
        if (UnbilledBalance != unbilledBalance)
            return false;

        return _activeStatement == seededActiveStatement;
    }

    public void Apply(CreditCardAccountOpened e)
    {
        Id = new CreditCardAccountId(e.CreditCardAccountId);
        UserId = new UserId(e.UserId);
        Name = e.Name;
        Currency = e.Currency;
        SettlementAccountId = new FundingAccountId(e.SettlementAccountId);
        BankProvider = e.BankProvider;
        ProductCode = e.ProductCode;
        CreditLimit = e.CreditLimit;
        StatementClosingDay = new StatementClosingDay(e.StatementClosingDay);
        GracePeriodDays = new GracePeriodDays(e.GracePeriodDays);
        Color = e.Color;
        LastFourDigits = e.LastFourDigits;
        IsActive = true;
    }

    public void Apply(CreditCardAccountUpdated e)
    {
        Name = e.Name;
        Currency = e.Currency;
        SettlementAccountId = new FundingAccountId(e.SettlementAccountId);
        BankProvider = e.BankProvider;
        ProductCode = e.ProductCode;
        CreditLimit = e.CreditLimit;
        StatementClosingDay = new StatementClosingDay(e.StatementClosingDay);
        GracePeriodDays = new GracePeriodDays(e.GracePeriodDays);
        Color = e.Color;
        LastFourDigits = e.LastFourDigits;
    }

    public void Apply(CreditCardAccountClosed e)
    {
        IsActive = false;
    }

    public void Apply(CreditCardStateSeeded e)
    {
        UnbilledBalance = e.UnbilledBalance;
        _activeStatementId = null;

        _activeStatement = ActiveStatementState.FromSeed(
            e.ActiveStatementBalance,
            e.ActiveStatementMinimumPaymentDue,
            e.ActiveStatementPeriodCloseDate,
            e.ActiveStatementDueDate);

        if (_activeStatement == ActiveStatementState.None)
            return;

        var statementId = new CreditCardStatementId(e.ActiveStatementId ?? "stmt-1");
        var periodCloseDate = _activeStatement.PeriodCloseDate!.Value;
        var dueDate = _activeStatement.DueDate!.Value;
        var periodFrom = periodCloseDate.AddMonths(-1).AddDays(1);

        _statements.Add(new CreditCardStatement(
            statementId!,
            periodFrom,
            periodCloseDate,
            periodCloseDate,
            dueDate,
            e.ActiveStatementBalance,
            e.ActiveStatementMinimumPaymentDue,
            "SEEDED",
            "SEEDED",
            e.UnbilledBalance,
            e.ActiveStatementBalance));
        _activeStatementId = statementId;
    }

    public void Apply(CreditCardStatementIssued e)
    {
        var statementId = new CreditCardStatementId(e.StatementId);
        var existing = _statements.SingleOrDefault(x => x.Id == statementId);
        if (existing is not null)
            return;

        _statements.Add(new CreditCardStatement(
            statementId,
            e.PeriodFrom,
            e.PeriodTo,
            e.StatementDate,
            e.DueDate,
            e.StatementBalance,
            e.MinimumPaymentDue,
            e.PolicyCode,
            e.PolicyVersion,
            e.UnbilledBalanceAfterIssue,
            e.StatementBalance));

        _activeStatementId = statementId;
        _activeStatement = ActiveStatementState.Open(
            e.StatementBalance,
            e.StatementBalance,
            e.MinimumPaymentDue,
            e.PeriodTo,
            e.DueDate);
        UnbilledBalance = e.UnbilledBalanceAfterIssue;
    }

    public void Apply(CreditCardStatementPaymentApplied e)
    {
        var statement = _statements.SingleOrDefault(x => x.Id.Value == e.StatementId)
            ?? throw new StatementNotFoundException();

        statement.ApplyPayment(new TransferId(e.TransferId), e.Amount, e.AppliedAtUtc);

        if (_activeStatementId?.Value == e.StatementId)
            _activeStatement = _activeStatement.WithOutstandingBalance(statement.OutstandingBalance);
    }
}

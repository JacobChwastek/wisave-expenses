using WiSave.Expenses.Contracts.Events.FundingAccounts;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Funding;

public sealed class FundingAccount : AggregateRoot<FundingAccountId>, IAggregateStream<FundingAccountId>
{
    private readonly List<PaymentInstrument> _paymentInstruments = [];

    public UserId UserId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public FundingAccountKind Kind { get; private set; }
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<PaymentInstrument> PaymentInstruments => _paymentInstruments.AsReadOnly();

    public static string ToStreamId(FundingAccountId id) => $"funding-account-{id.Value}";

    public FundingAccount() { }

    public static FundingAccount Open(
        FundingAccountId id,
        UserId userId,
        string name,
        FundingAccountKind kind,
        Currency currency,
        decimal openingBalance,
        string? color)
    {
        EnsureValidName(name);

        if (openingBalance < 0m)
            throw new DomainException("Opening balance cannot be negative.");

        var account = new FundingAccount();
        account.RaiseEvent(new FundingAccountOpened(
            id.Value,
            userId.Value,
            name,
            kind,
            currency,
            openingBalance,
            color,
            DateTimeOffset.UtcNow));
        return account;
    }

    public void Reconfigure(string name, FundingAccountKind kind, Currency currency, string? color)
    {
        EnsureActive();
        EnsureValidName(name);

        RaiseEvent(new FundingAccountUpdated(
            Id.Value,
            UserId.Value,
            name,
            kind,
            currency,
            color,
            DateTimeOffset.UtcNow));
    }

    public void Close()
    {
        EnsureActive();
        RaiseEvent(new FundingAccountClosed(Id.Value, UserId.Value, DateTimeOffset.UtcNow));
    }

    public void AddPaymentInstrument(
        PaymentInstrumentId paymentInstrumentId,
        PaymentInstrumentKind kind,
        string name,
        string? lastFourDigits,
        string? network,
        string? color)
    {
        EnsureActive();
        PaymentInstrument.EnsureValidName(name);
        PaymentInstrument.EnsureValidLastFourDigits(lastFourDigits);

        if (_paymentInstruments.Any(x => x.Id == paymentInstrumentId))
            throw new DomainException("Payment instrument already exists.");

        RaiseEvent(new FundingPaymentInstrumentAdded(
            Id.Value,
            UserId.Value,
            paymentInstrumentId.Value,
            name,
            kind,
            lastFourDigits,
            network,
            color,
            DateTimeOffset.UtcNow));
    }

    public void UpdatePaymentInstrument(
        PaymentInstrumentId paymentInstrumentId,
        PaymentInstrumentKind kind,
        string name,
        string? lastFourDigits,
        string? network,
        string? color)
    {
        EnsureActive();
        PaymentInstrument.EnsureValidName(name);
        PaymentInstrument.EnsureValidLastFourDigits(lastFourDigits);
        EnsureActivePaymentInstrumentExists(paymentInstrumentId);

        RaiseEvent(new FundingPaymentInstrumentUpdated(
            Id.Value,
            UserId.Value,
            paymentInstrumentId.Value,
            name,
            kind,
            lastFourDigits,
            network,
            color,
            DateTimeOffset.UtcNow));
    }

    public void RemovePaymentInstrument(PaymentInstrumentId paymentInstrumentId)
    {
        EnsureActive();
        EnsureActivePaymentInstrumentExists(paymentInstrumentId);

        RaiseEvent(new FundingPaymentInstrumentRemoved(
            Id.Value,
            UserId.Value,
            paymentInstrumentId.Value,
            DateTimeOffset.UtcNow));
    }

    public void PostTransfer(
        TransferId transferId,
        decimal amount,
        DateTimeOffset postedAtUtc)
    {
        EnsureActive();

        if (amount <= 0m)
            throw new DomainException("Funding transfer amount must be greater than zero.");

        if (amount > Balance)
            throw new DomainException("Funding transfer amount cannot exceed account balance.");

        RaiseEvent(new FundingTransferPosted(
            Id.Value,
            UserId.Value,
            transferId.Value,
            amount,
            postedAtUtc,
            DateTimeOffset.UtcNow));
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify a closed funding account.");
    }

    private static void EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Funding account name is required.");
    }

    private void EnsureActivePaymentInstrumentExists(PaymentInstrumentId paymentInstrumentId)
    {
        if (_paymentInstruments.All(x => x.Id != paymentInstrumentId || !x.IsActive))
            throw new DomainException("Payment instrument not found.");
    }

    public void Apply(FundingAccountOpened e)
    {
        Id = new FundingAccountId(e.FundingAccountId);
        UserId = new UserId(e.UserId);
        Name = e.Name;
        Kind = e.Kind;
        Currency = e.Currency;
        Balance = e.OpeningBalance;
        Color = e.Color;
        IsActive = true;
    }

    public void Apply(FundingAccountUpdated e)
    {
        Name = e.Name;
        Kind = e.Kind;
        Currency = e.Currency;
        Color = e.Color;
    }

    public void Apply(FundingAccountClosed e)
    {
        IsActive = false;
    }

    public void Apply(FundingPaymentInstrumentAdded e)
    {
        _paymentInstruments.Add(new PaymentInstrument(
            new PaymentInstrumentId(e.PaymentInstrumentId),
            e.Kind,
            e.Name,
            e.LastFourDigits,
            e.Network,
            e.Color,
            isActive: true));
    }

    public void Apply(FundingPaymentInstrumentUpdated e)
    {
        var instrument = _paymentInstruments.Single(x => x.Id == new PaymentInstrumentId(e.PaymentInstrumentId));
        instrument.Update(e.Kind, e.Name, e.LastFourDigits, e.Network, e.Color);
    }

    public void Apply(FundingPaymentInstrumentRemoved e)
    {
        var instrument = _paymentInstruments.Single(x => x.Id == new PaymentInstrumentId(e.PaymentInstrumentId));
        instrument.Remove();
    }

    public void Apply(FundingTransferPosted e)
    {
        Balance -= e.Amount;
    }
}

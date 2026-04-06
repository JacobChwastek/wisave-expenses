using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.Funding;

public sealed class PaymentInstrument
{
    public PaymentInstrumentId Id { get; }
    public PaymentInstrumentKind Kind { get; private set; }
    public string Name { get; private set; }
    public string? LastFourDigits { get; private set; }
    public string? Network { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; }

    public PaymentInstrument(
        PaymentInstrumentId id,
        PaymentInstrumentKind kind,
        string name,
        string? lastFourDigits,
        string? network,
        string? color,
        bool isActive)
    {
        EnsureValidName(name);
        EnsureValidLastFourDigits(lastFourDigits);

        Id = id;
        Kind = kind;
        Name = name;
        LastFourDigits = lastFourDigits;
        Network = network;
        Color = color;
        IsActive = isActive;
    }

    public void Update(
        PaymentInstrumentKind kind,
        string name,
        string? lastFourDigits,
        string? network,
        string? color)
    {
        EnsureValidName(name);
        EnsureValidLastFourDigits(lastFourDigits);

        Kind = kind;
        Name = name;
        LastFourDigits = lastFourDigits;
        Network = network;
        Color = color;
        IsActive = true;
    }

    public void Remove() => IsActive = false;

    public static void EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Payment instrument name is required.");
    }

    public static void EnsureValidLastFourDigits(string? lastFourDigits)
    {
        if (lastFourDigits is null)
            return;

        if (lastFourDigits.Length != 4 || lastFourDigits.Any(x => !char.IsDigit(x)))
            throw new DomainException("Payment instrument last four digits must contain exactly four digits.");
    }
}

namespace WiSave.Expenses.Core.Domain.CreditCards.Specifications;

/// <summary>
/// Checks whether an already-applied transfer has the same statement allocations
/// as a requested settlement application.
/// </summary>
/// <remarks>
/// This specification supports idempotent retry handling. A retry is accepted
/// only when the same transfer targets the same statements with the same amounts.
/// </remarks>
public sealed class PaymentApplicationsMatchSpecification(
    IReadOnlyDictionary<string, decimal> expectedApplications)
{
    /// <summary>
    /// Evaluates whether the actual persisted applications match the expected set.
    /// </summary>
    /// <param name="actualApplications">Previously applied amounts keyed by statement id.</param>
    /// <returns><c>true</c> when both sets contain the same statement ids and amounts.</returns>
    public bool IsSatisfiedBy(IReadOnlyDictionary<string, decimal> actualApplications) =>
        actualApplications.Count == expectedApplications.Count
        && actualApplications.All(application =>
            expectedApplications.TryGetValue(application.Key, out var amount)
            && amount == application.Value);
}

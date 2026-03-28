using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Events;

public sealed record BudgetCreated(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring,
    DateTimeOffset Timestamp);

public sealed record BudgetCopiedFromPrevious(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    int SourceMonth,
    int SourceYear,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring,
    Dictionary<string, decimal> CategoryLimits,
    DateTimeOffset Timestamp);

public sealed record OverallLimitSet(
    string BudgetId,
    string UserId,
    decimal TotalLimit,
    DateTimeOffset Timestamp);

public sealed record CategoryLimitSet(
    string BudgetId,
    string UserId,
    string CategoryId,
    decimal Limit,
    DateTimeOffset Timestamp);

public sealed record CategoryLimitRemoved(
    string BudgetId,
    string UserId,
    string CategoryId,
    DateTimeOffset Timestamp);

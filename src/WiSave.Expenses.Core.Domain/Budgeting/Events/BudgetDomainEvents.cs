namespace WiSave.Expenses.Core.Domain.Budgeting.Events;

public sealed record BudgetCreatedEvent(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    decimal TotalLimit,
    string Currency,
    bool Recurring);

public sealed record BudgetCopiedFromPreviousEvent(
    string BudgetId,
    string UserId,
    int Month,
    int Year,
    int SourceMonth,
    int SourceYear,
    List<CategoryBudgetSnapshot> CategoryBudgets);

public sealed record CategoryBudgetSnapshot(string CategoryId, decimal Limit);

public sealed record OverallLimitSetEvent(string BudgetId, decimal TotalLimit);

public sealed record CategoryLimitSetEvent(string BudgetId, string CategoryId, decimal Limit);

public sealed record CategoryLimitRemovedEvent(string BudgetId, string CategoryId);

public sealed record RecurringToggledEvent(string BudgetId, bool Recurring);

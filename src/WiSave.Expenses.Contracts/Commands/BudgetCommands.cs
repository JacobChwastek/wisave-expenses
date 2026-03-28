using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands;

public sealed record CreateBudget(
    Guid CorrelationId,
    string UserId,
    int Month,
    int Year,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring = true);

public sealed record CopyBudgetFromPrevious(
    Guid CorrelationId,
    string UserId,
    int Month,
    int Year);

public sealed record SetOverallLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    decimal TotalLimit);

public sealed record SetCategoryLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    string CategoryId,
    decimal Limit);

public sealed record RemoveCategoryLimit(
    Guid CorrelationId,
    string UserId,
    string BudgetId,
    string CategoryId);

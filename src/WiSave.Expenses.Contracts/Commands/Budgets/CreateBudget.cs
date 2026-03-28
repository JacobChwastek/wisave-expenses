using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Contracts.Commands.Budgets;

public sealed record CreateBudget(
    Guid CorrelationId,
    string UserId,
    int Month,
    int Year,
    decimal TotalLimit,
    Currency Currency,
    bool Recurring = true);

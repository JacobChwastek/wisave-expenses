using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.Budgets;

public sealed record CreateBudgetRequest(int Month, int Year, decimal TotalLimit, Currency Currency, bool Recurring = true);

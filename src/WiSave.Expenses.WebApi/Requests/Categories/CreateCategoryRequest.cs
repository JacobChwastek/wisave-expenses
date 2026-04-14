namespace WiSave.Expenses.WebApi.Requests.Categories;

public sealed record CreateCategoryRequest(string Name, int? SortOrder = null);

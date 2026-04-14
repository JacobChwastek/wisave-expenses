namespace WiSave.Expenses.WebApi.Requests.Categories;

public sealed record UpdateCategoryRequest(string Name, int? SortOrder = null);

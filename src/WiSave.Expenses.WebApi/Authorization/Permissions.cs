namespace WiSave.Expenses.WebApi.Authorization;

public static class Permissions
{
    public static class Expenses
    {
        public const string Read = "expenses:read";
        public const string Write = "expenses:write";
        public const string Delete = "expenses:delete";
    }
}

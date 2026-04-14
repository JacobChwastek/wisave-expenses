using System.Reflection;

namespace WiSave.Expenses.WebApi.Tests.Authorization;

public class PermissionConstantsTests
{
    [Fact]
    public void Permissions_Class_ExposesExpensesPermissionValues()
    {
        var type = typeof(WiSave.Expenses.WebApi.Authorization.PermissionMetadata).Assembly
            .GetType("WiSave.Expenses.WebApi.Authorization.Permissions+Expenses");

        Assert.NotNull(type);
        Assert.Equal("expenses:read", GetFieldValue(type!, "Read"));
        Assert.Equal("expenses:write", GetFieldValue(type, "Write"));
        Assert.Equal("expenses:delete", GetFieldValue(type, "Delete"));
    }

    [Fact]
    public void Permissions_Class_DoesNotExposeTestOnlyPermissions()
    {
        var type = typeof(WiSave.Expenses.WebApi.Authorization.PermissionMetadata).Assembly
            .GetType("WiSave.Expenses.WebApi.Authorization.Permissions");

        Assert.NotNull(type);
        Assert.Null(type!.GetNestedType("Tests", BindingFlags.Public));
        Assert.Null(type.GetField("TestRead", BindingFlags.Public | BindingFlags.Static));
    }

    private static string? GetFieldValue(Type type, string fieldName)
    {
        return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue() as string;
    }
}

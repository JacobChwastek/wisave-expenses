using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections;

namespace WiSave.Expenses.Projections.Tests;

internal static class TestDbContextFactory
{
    public static ProjectionsDbContext Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProjectionsDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ProjectionsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}

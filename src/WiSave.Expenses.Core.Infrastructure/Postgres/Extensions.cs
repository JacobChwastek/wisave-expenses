using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Core.Application.Abstractions;

namespace WiSave.Expenses.Core.Infrastructure.Postgres;

public static class PostgresExtensions
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ExpensesDbContext>(opts => opts.UseNpgsql(connectionString));
        services.AddScoped<ICategoryRepository, PostgresCategoryRepository>();

        return services;
    }
}

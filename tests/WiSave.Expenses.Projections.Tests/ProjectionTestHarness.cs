using MassTransit;
using MassTransit.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Expenses.Projections.EventHandlers;

namespace WiSave.Expenses.Projections.Tests;

internal sealed class ProjectionTestHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private ProjectionTestHarness(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
        Harness = provider.GetRequiredService<ITestHarness>();
    }

    public ITestHarness Harness { get; }

    public static async Task<ProjectionTestHarness> StartAsync()
    {
        var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<ProjectionsDbContext>(opts => opts.UseSqlite(connectionString));
        services.AddScoped(typeof(IdempotentProjectionFilter<>));
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<BudgetEventHandler>();
            cfg.AddConsumer<CreditCardAccountEventHandler>();
            cfg.AddConsumer<ExpenseEventHandler>();
            cfg.AddConsumer<FundingAccountEventHandler>();
            cfg.UsingInMemory((context, bus) =>
            {
                bus.UseConsumeFilter(typeof(IdempotentProjectionFilter<>), context);
                bus.ConfigureEndpoints(context);
            });
        });

        var provider = services.BuildServiceProvider(true);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProjectionsDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var testHarness = new ProjectionTestHarness(connection, provider);
        await testHarness.Harness.Start();
        return testHarness;
    }

    public Task PublishAsync<T>(T message) where T : class =>
        Harness.Bus.Publish(message, context => context.MessageId = Guid.NewGuid());

    public async Task<T> QueryAsync<T>(Func<ProjectionsDbContext, Task<T>> query)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectionsDbContext>();
        return await query(db);
    }

    public async Task EventuallyAsync(Func<ProjectionsDbContext, Task> assertion)
    {
        Exception? lastException = null;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await using var scope = _provider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ProjectionsDbContext>();
                await assertion(db);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(50);
            }
        }

        throw new TimeoutException("Projection assertion did not pass within the timeout.", lastException);
    }

    public async ValueTask DisposeAsync()
    {
        await Harness.Stop();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

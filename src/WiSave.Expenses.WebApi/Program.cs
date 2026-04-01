using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Core.Infrastructure.Postgres;
using WiSave.Expenses.Projections;
using WiSave.Expenses.Projections.Queries;
using WiSave.Expenses.WebApi.Endpoints;
using WiSave.Expenses.WebApi.Endpoints.Categories;
using WiSave.Expenses.WebApi.Endpoints.Expenses;

var builder = WebApplication.CreateBuilder(args);

var postgresConfig = builder.Configuration.GetConnectionString("Config")
    ?? "Host=localhost;Port=5433;Database=wisave_expenses;Username=wisave;Password=wisave_dev";
var postgresProjections = builder.Configuration.GetConnectionString("Projections")
    ?? postgresConfig;

// Config DB (categories)
builder.Services.AddDbContext<ExpensesDbContext>(opts => opts.UseNpgsql(postgresConfig));

// Projections DB (read models)
builder.Services.AddDbContext<ProjectionsDbContext>(opts => opts.UseNpgsql(postgresProjections));

// Identity
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();

// Query services
builder.Services.AddScoped<AccountQueries>();
builder.Services.AddScoped<ExpenseQueries>();
builder.Services.AddScoped<BudgetQueries>();

// MassTransit (publish only — no consumers in WebApi)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "expenses";

        cfg.Host(host, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
    });
});

var app = builder.Build();

// Endpoints
app.MapAccountEndpoints();
app.MapExpenseEndpoints();
app.MapBudgetEndpoints();
app.MapCategoryEndpoints();

app.Run();

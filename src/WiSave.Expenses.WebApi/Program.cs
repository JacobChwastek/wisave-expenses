using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Core.Infrastructure.Messaging;
using WiSave.Expenses.Core.Infrastructure.Postgres;
using WiSave.Expenses.Projections;
using WiSave.Expenses.Projections.Repositories;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Endpoints;
using WiSave.Expenses.WebApi.Json;

var builder = WebApplication.CreateBuilder(args);

var postgresCs = builder.Configuration.GetConnectionString("Postgres")!;

// Config DB (categories) — schema: config
builder.Services.AddDbContext<ExpensesDbContext>(opts => opts.UseNpgsql(postgresCs));

// Projections DB (read models) — schema: projections
builder.Services.AddDbContext<ProjectionsDbContext>(opts => opts.UseNpgsql(postgresCs));

// Identity
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();
builder.Services.AddScoped<PermissionContext>();

// Read repositories
builder.Services.AddScoped<FundingAccountReadRepository>();
builder.Services.AddScoped<ExpenseReadRepository>();

builder.Services.AddExpensesJson();
builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

// Endpoints
app.MapFundingAccountEndpoints();
app.MapExpenseEndpoints();
app.MapCategoryEndpoints();

app.Run();

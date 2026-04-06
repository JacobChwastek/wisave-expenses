using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WiSave.Expenses.Projections.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "projections");

            migrationBuilder.CreateTable(
                name: "budget_category_limits",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    Limit = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_category_limits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TotalLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Recurring = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "checkpoints",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_accounts",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SettlementAccountId = table.Column<string>(type: "text", nullable: false),
                    BankProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StatementClosingDay = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    UnbilledBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActiveStatementBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActiveStatementOutstandingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActiveStatementMinimumPaymentDue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActiveStatementDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActiveStatementPeriodCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    LastFourDigits = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_statements",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreditCardAccountId = table.Column<string>(type: "text", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StatementBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumPaymentDue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_statements", x => new { x.CreditCardAccountId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    SubcategoryId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Recurring = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "funding_accounts",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Color = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funding_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_stats",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_stats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                schema: "projections",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "spending_summaries",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spending_summaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_category_limits_BudgetId",
                schema: "projections",
                table: "budget_category_limits",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_budget_category_limits_BudgetId_CategoryId",
                schema: "projections",
                table: "budget_category_limits",
                columns: new[] { "BudgetId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budgets_UserId_Month_Year",
                schema: "projections",
                table: "budgets",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_accounts_SettlementAccountId",
                schema: "projections",
                table: "credit_card_accounts",
                column: "SettlementAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_accounts_UserId",
                schema: "projections",
                table: "credit_card_accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_statements_CreditCardAccountId",
                schema: "projections",
                table: "credit_card_statements",
                column: "CreditCardAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_AccountId",
                schema: "projections",
                table: "expenses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_CategoryId",
                schema: "projections",
                table: "expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_Date",
                schema: "projections",
                table: "expenses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_UserId",
                schema: "projections",
                table: "expenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_funding_accounts_UserId",
                schema: "projections",
                table: "funding_accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_stats_UserId_Year",
                schema: "projections",
                table: "monthly_stats",
                columns: new[] { "UserId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_stats_UserId_Year_Month",
                schema: "projections",
                table: "monthly_stats",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spending_summaries_UserId_Month_Year",
                schema: "projections",
                table: "spending_summaries",
                columns: new[] { "UserId", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_spending_summaries_UserId_Month_Year_CategoryId",
                schema: "projections",
                table: "spending_summaries",
                columns: new[] { "UserId", "Month", "Year", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_category_limits",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "budgets",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "checkpoints",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "credit_card_accounts",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "credit_card_statements",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "funding_accounts",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "monthly_stats",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "processed_messages",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "spending_summaries",
                schema: "projections");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WiSave.Expenses.Projections.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class PaymentInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "funding_payment_instruments",
                schema: "projections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FundingAccountId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Network = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funding_payment_instruments", x => new { x.FundingAccountId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_funding_payment_instruments_FundingAccountId",
                schema: "projections",
                table: "funding_payment_instruments",
                column: "FundingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_funding_payment_instruments_UserId",
                schema: "projections",
                table: "funding_payment_instruments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "funding_payment_instruments",
                schema: "projections");
        }
    }
}

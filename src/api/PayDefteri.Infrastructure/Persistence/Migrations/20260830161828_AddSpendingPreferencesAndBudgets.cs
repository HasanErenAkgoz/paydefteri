using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayDefteri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpendingPreferencesAndBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spending_category_budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spending_category_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spending_merchant_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MerchantKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spending_merchant_preferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_spending_category_budgets_OwnerUserId_Month_Category_Curren~",
                table: "spending_category_budgets",
                columns: new[] { "OwnerUserId", "Month", "Category", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spending_merchant_preferences_OwnerUserId_MerchantKey",
                table: "spending_merchant_preferences",
                columns: new[] { "OwnerUserId", "MerchantKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spending_category_budgets");

            migrationBuilder.DropTable(
                name: "spending_merchant_preferences");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuzulTaksitTakip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpensePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expense_payments_expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_expense_payments_partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_payments_ExpenseId_PartnerId",
                table: "expense_payments",
                columns: new[] { "ExpenseId", "PartnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_payments_PartnerId",
                table: "expense_payments",
                column: "PartnerId");

            // Backfill legacy single-payer column into expense_payments.
            migrationBuilder.Sql(
                """
                INSERT INTO expense_payments ("Id", "ExpenseId", "PartnerId", "Amount")
                SELECT gen_random_uuid(), e."Id", e."PaidByPartnerId", e."TotalAmount"
                FROM expenses e
                WHERE e."PaidByPartnerId" IS NOT NULL
                  AND e."Status" = 'Paid'
                  AND NOT EXISTS (
                    SELECT 1 FROM expense_payments p WHERE p."ExpenseId" = e."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_payments");
        }
    }
}

using PayDefteri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayDefteri.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806010000_AddExpenseBoardIndex")]
public partial class AddExpenseBoardIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_expenses_PlanId_OccurredOn_CreatedAtUtc",
            table: "expenses",
            columns: new[] { "PlanId", "OccurredOn", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_expenses_PlanId_OccurredOn_CreatedAtUtc",
            table: "expenses");
    }
}

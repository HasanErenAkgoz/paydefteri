using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayDefteri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "ReminderDaysAfter",
                table: "plans",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "ReminderDaysBefore",
                table: "plans",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<bool>(
                name: "RemindersEnabled",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "payment_reminder_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OffsetDays = table.Column<int>(type: "integer", nullable: false),
                    SentOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_reminder_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_reminder_logs_installments_InstallmentId",
                        column: x => x.InstallmentId,
                        principalTable: "installments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_reminder_logs_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_reminder_logs_InstallmentId",
                table: "payment_reminder_logs",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_reminder_logs_PlanId_InstallmentId_PartnerId_Kind_O~",
                table: "payment_reminder_logs",
                columns: new[] { "PlanId", "InstallmentId", "PartnerId", "Kind", "OffsetDays", "SentOn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_reminder_logs");

            migrationBuilder.DropColumn(
                name: "ReminderDaysAfter",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "ReminderDaysBefore",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "RemindersEnabled",
                table: "plans");
        }
    }
}

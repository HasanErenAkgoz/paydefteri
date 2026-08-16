using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayDefteri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptAndIbanSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IbanMode",
                table: "plans",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequireReceipt",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SettlementIban",
                table: "plans",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptContentType",
                table: "payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFileName",
                table: "payments",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptStorageKey",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptUploadedAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "partners",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IbanMode",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "RequireReceipt",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "SettlementIban",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "ReceiptContentType",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ReceiptFileName",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ReceiptStorageKey",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ReceiptUploadedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "partners");
        }
    }
}

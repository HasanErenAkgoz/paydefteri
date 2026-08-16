using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayDefteri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartnerInviteEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteEmail",
                table: "partners",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_partners_PlanId_InviteEmail",
                table: "partners",
                columns: new[] { "PlanId", "InviteEmail" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_partners_PlanId_InviteEmail",
                table: "partners");

            migrationBuilder.DropColumn(
                name: "InviteEmail",
                table: "partners");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuzulTaksitTakip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanMembersAndInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedUserId",
                table: "partners",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "plan_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvitedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_invites_partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_invites_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_members_partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_plan_members_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_partners_PlanId_LinkedUserId",
                table: "partners",
                columns: new[] { "PlanId", "LinkedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_invites_PartnerId",
                table: "plan_invites",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_invites_PlanId_Email_Status",
                table: "plan_invites",
                columns: new[] { "PlanId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_invites_Token",
                table: "plan_invites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_members_PartnerId",
                table: "plan_members",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_members_PlanId_UserId",
                table: "plan_members",
                columns: new[] { "PlanId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_invites");

            migrationBuilder.DropTable(
                name: "plan_members");

            migrationBuilder.DropIndex(
                name: "IX_partners_PlanId_LinkedUserId",
                table: "partners");

            migrationBuilder.DropColumn(
                name: "LinkedUserId",
                table: "partners");
        }
    }
}

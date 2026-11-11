using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeUserSoftDeleteAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackOfficeUsers_Email",
                table: "BackOfficeUsers");

            migrationBuilder.DropIndex(
                name: "IX_BackOfficeUsers_UserName",
                table: "BackOfficeUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BackOfficeUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BackOfficeAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeAuditLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BackOfficeUsers",
                keyColumn: "Id",
                keyValue: new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_Email",
                table: "BackOfficeUsers",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_UserName",
                table: "BackOfficeUsers",
                column: "UserName",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeAuditLogs_ActorUserId",
                table: "BackOfficeAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeAuditLogs_EntityType_EntityId",
                table: "BackOfficeAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeAuditLogs_OccurredAtUtc",
                table: "BackOfficeAuditLogs",
                column: "OccurredAtUtc",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackOfficeAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_BackOfficeUsers_Email",
                table: "BackOfficeUsers");

            migrationBuilder.DropIndex(
                name: "IX_BackOfficeUsers_UserName",
                table: "BackOfficeUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BackOfficeUsers");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_Email",
                table: "BackOfficeUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_UserName",
                table: "BackOfficeUsers",
                column: "UserName",
                unique: true);
        }
    }
}

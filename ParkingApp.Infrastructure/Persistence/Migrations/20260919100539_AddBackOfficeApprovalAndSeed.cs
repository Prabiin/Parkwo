using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeApprovalAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VerificationStatus",
                table: "ParkingProviders",
                newName: "ApprovalStatus");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "BackOfficeUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeUsers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BackOfficeUsers",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Email", "FullName", "IsActive", "LastLoginAtUtc", "PasswordHash", "UpdatedAtUtc", "UpdatedBy", "UserName" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "superadmin@gmail.com", "Super Admin", true, null, "100000.EBESExQVFhcYGRobHB0eHw==.u3Fyc5ZjygvMHMkQlV122zvEFBsQzs5gNbr5qrTujjk=", null, null, "SuperAdmin" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackOfficeUsers");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Organizations");

            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "ParkingProviders",
                newName: "VerificationStatus");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlterBackOfficeUserRoleToOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the composite primary key and add single-column primary key on UserId
            // This preserves existing data - UserId is already unique in current data
            migrationBuilder.DropPrimaryKey(
                name: "PK_BackOfficeUserRoles",
                table: "BackOfficeUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackOfficeUserRoles",
                table: "BackOfficeUserRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BackOfficeUserRoles",
                table: "BackOfficeUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackOfficeUserRoles",
                table: "BackOfficeUserRoles",
                columns: new[] { "UserId", "RoleId" });
        }
    }
}

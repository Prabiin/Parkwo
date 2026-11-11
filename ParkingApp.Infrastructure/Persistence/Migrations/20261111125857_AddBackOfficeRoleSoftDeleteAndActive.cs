using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeRoleSoftDeleteAndActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackOfficeRoles_Name",
                table: "BackOfficeRoles");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BackOfficeRoles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BackOfficeRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "BackOfficeRoles",
                keyColumn: "Id",
                keyValue: new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c"),
                column: "IsActive",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeRoles_Name",
                table: "BackOfficeRoles",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackOfficeRoles_Name",
                table: "BackOfficeRoles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BackOfficeRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BackOfficeRoles");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeRoles_Name",
                table: "BackOfficeRoles",
                column: "Name",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDrivingLicenseAndVehicleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VehicleCategory",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DrivingLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Categories = table.Column<int[]>(type: "integer[]", nullable: false),
                    FrontImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BackImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrivingLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrivingLicenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrivingLicenses_LicenseNumber",
                table: "DrivingLicenses",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrivingLicenses_UserId",
                table: "DrivingLicenses",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrivingLicenses");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleCategory",
                table: "Vehicles");
        }
    }
}

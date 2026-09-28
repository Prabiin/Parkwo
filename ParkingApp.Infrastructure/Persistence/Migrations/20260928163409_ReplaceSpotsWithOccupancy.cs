using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSpotsWithOccupancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSpots");

            migrationBuilder.AddColumn<int>(
                name: "FourWheelerOccupancy",
                table: "ParkingFacilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FourWheelerPricePerHourNpr",
                table: "ParkingFacilities",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LandAreaSqM",
                table: "ParkingFacilities",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingFourWheelerOccupancy",
                table: "ParkingFacilities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingLandAreaSqM",
                table: "ParkingFacilities",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingTwoWheelerOccupancy",
                table: "ParkingFacilities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TwoWheelerOccupancy",
                table: "ParkingFacilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TwoWheelerPricePerHourNpr",
                table: "ParkingFacilities",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FourWheelerOccupancy",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "FourWheelerPricePerHourNpr",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "LandAreaSqM",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "PendingFourWheelerOccupancy",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "PendingLandAreaSqM",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "PendingTwoWheelerOccupancy",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "TwoWheelerOccupancy",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "TwoWheelerPricePerHourNpr",
                table: "ParkingFacilities");

            migrationBuilder.CreateTable(
                name: "ParkingSpots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PricePerHourNpr = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SpotNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSpots_ParkingFacilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "ParkingFacilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_FacilityId",
                table: "ParkingSpots",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_FacilityId_SpotNumber",
                table: "ParkingSpots",
                columns: new[] { "FacilityId", "SpotNumber" },
                unique: true);
        }
    }
}

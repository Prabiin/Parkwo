using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityMarkedFlagAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasMarkedParkingLot",
                table: "ParkingFacilities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "ParkingFacilities",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""ParkingFacilities"" SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography WHERE ""Latitude"" IS NOT NULL AND ""Longitude"" IS NOT NULL AND ""Location"" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingFacilities_Location",
                table: "ParkingFacilities",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingFacilities_Location",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "HasMarkedParkingLot",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ParkingFacilities");
        }
    }
}

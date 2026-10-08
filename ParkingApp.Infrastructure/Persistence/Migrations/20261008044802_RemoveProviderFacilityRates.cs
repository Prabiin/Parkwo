using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProviderFacilityRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FourWheelerPricePerHourNpr",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "TwoWheelerPricePerHourNpr",
                table: "ParkingFacilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FourWheelerPricePerHourNpr",
                table: "ParkingFacilities",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TwoWheelerPricePerHourNpr",
                table: "ParkingFacilities",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

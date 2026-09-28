using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ParkingProviders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ParkingFacilities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "DrivingLicenses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ParkingProviders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ParkingFacilities");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "DrivingLicenses");
        }
    }
}

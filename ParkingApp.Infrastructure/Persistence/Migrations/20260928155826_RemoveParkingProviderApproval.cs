using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveParkingProviderApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ParkingProviders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ParkingProviders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ParkingProviders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ParkingProviders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}

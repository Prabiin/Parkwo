using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedParkingsAndOptionalVehicleCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VehicleCategory",
                table: "Vehicles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "SavedParkings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    FreeParkingFacilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedParkings", x => x.Id);
                    table.CheckConstraint("CK_SavedParkings_ExactlyOneTarget", "(\"FacilityId\" IS NOT NULL)::int + (\"FreeParkingFacilityId\" IS NOT NULL)::int = 1");
                    table.ForeignKey(
                        name: "FK_SavedParkings_FreeParkingFacilities_FreeParkingFacilityId",
                        column: x => x.FreeParkingFacilityId,
                        principalTable: "FreeParkingFacilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedParkings_ParkingFacilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "ParkingFacilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedParkings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedParkings_FacilityId",
                table: "SavedParkings",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedParkings_FreeParkingFacilityId",
                table: "SavedParkings",
                column: "FreeParkingFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedParkings_UserId_FacilityId",
                table: "SavedParkings",
                columns: new[] { "UserId", "FacilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedParkings_UserId_FreeParkingFacilityId",
                table: "SavedParkings",
                columns: new[] { "UserId", "FreeParkingFacilityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedParkings");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleCategory",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

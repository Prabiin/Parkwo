using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedManuallyByStaffId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedManuallyByStaffId",
                table: "OverstayPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnteredByStaffId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExitedByStaffId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenedByStaffId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DeviceBrand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staffs_ParkingFacilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "ParkingFacilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffRefreshTokens_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EnteredByStaffId",
                table: "Bookings",
                column: "EnteredByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ExitedByStaffId",
                table: "Bookings",
                column: "ExitedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OpenedByStaffId",
                table: "Bookings",
                column: "OpenedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRefreshTokens_StaffId",
                table: "StaffRefreshTokens",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRefreshTokens_Token",
                table: "StaffRefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Email",
                table: "Staffs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Facility_Status",
                table: "Staffs",
                columns: new[] { "FacilityId", "IsDeleted", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staffs_EnteredByStaffId",
                table: "Bookings",
                column: "EnteredByStaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staffs_ExitedByStaffId",
                table: "Bookings",
                column: "ExitedByStaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staffs_OpenedByStaffId",
                table: "Bookings",
                column: "OpenedByStaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staffs_EnteredByStaffId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staffs_ExitedByStaffId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staffs_OpenedByStaffId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "StaffRefreshTokens");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_EnteredByStaffId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ExitedByStaffId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_OpenedByStaffId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VerifiedManuallyByStaffId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VerifiedManuallyByStaffId",
                table: "OverstayPayments");

            migrationBuilder.DropColumn(
                name: "EnteredByStaffId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExitedByStaffId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OpenedByStaffId",
                table: "Bookings");
        }
    }
}

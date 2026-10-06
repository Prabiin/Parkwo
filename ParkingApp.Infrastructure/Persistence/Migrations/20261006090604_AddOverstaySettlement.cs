using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOverstaySettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OverstayAmountPaisa",
                table: "Bookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverstayBillableHours",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OverstayPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gateway = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountPaisa = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    GatewayPaymentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverstayPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverstayPayments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Unsettled_Overstay",
                table: "Bookings",
                column: "UserId",
                filter: "\"OverstayAmountPaisa\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OverstayPayments_BookingId",
                table: "OverstayPayments",
                column: "BookingId",
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_OverstayPayments_GatewayPaymentId",
                table: "OverstayPayments",
                column: "GatewayPaymentId",
                unique: true,
                filter: "\"GatewayPaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OverstayPayments_GatewayTransactionId",
                table: "OverstayPayments",
                column: "GatewayTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OverstayPayments");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Unsettled_Overstay",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OverstayAmountPaisa",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OverstayBillableHours",
                table: "Bookings");
        }
    }
}

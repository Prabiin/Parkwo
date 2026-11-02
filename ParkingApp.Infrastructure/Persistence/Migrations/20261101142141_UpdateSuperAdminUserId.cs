using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuperAdminUserId : Migration
    {
        // New random GUID for SuperAdmin user
        private const string NewSuperAdminId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
        private const string OldSuperAdminId = "00000000-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE ""BackOfficeUsers""
                SET ""Id"" = '{NewSuperAdminId}'::uuid
                WHERE ""Id"" = '{OldSuperAdminId}'::uuid;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE ""BackOfficeUsers""
                SET ""Id"" = '{OldSuperAdminId}'::uuid
                WHERE ""Id"" = '{NewSuperAdminId}'::uuid;
            ");
        }
    }
}

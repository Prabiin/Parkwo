using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateBackOfficeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackOfficeMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ParentMenuId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackOfficeMenus_BackOfficeMenus_ParentMenuId",
                        column: x => x.ParentMenuId,
                        principalTable: "BackOfficeMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BackOfficeRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackOfficeUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackOfficeMenuActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeMenuActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackOfficeUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_BackOfficeUserRoles_BackOfficeRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "BackOfficeRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackOfficeUserRoles_BackOfficeUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BackOfficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackOfficeRoleMenuActions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackOfficeRoleMenuActions", x => new { x.RoleId, x.MenuActionId });
                    table.ForeignKey(
                        name: "FK_BackOfficeRoleMenuActions_BackOfficeMenuActions_MenuActionId",
                        column: x => x.MenuActionId,
                        principalTable: "BackOfficeMenuActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackOfficeRoleMenuActions_BackOfficeRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "BackOfficeRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeMenus_Key",
                table: "BackOfficeMenus",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeMenus_ParentMenuId",
                table: "BackOfficeMenus",
                column: "ParentMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeMenuActions_MenuKey_ActionKey",
                table: "BackOfficeMenuActions",
                columns: new[] { "MenuKey", "ActionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeRoles_Name",
                table: "BackOfficeRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_UserName",
                table: "BackOfficeUsers",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUsers_Email",
                table: "BackOfficeUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeUserRoles_RoleId",
                table: "BackOfficeUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BackOfficeRoleMenuActions_MenuActionId",
                table: "BackOfficeRoleMenuActions",
                column: "MenuActionId");

            // Seed data - Menus
            var seedTime = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            var menuDashboardId = new Guid("a1f5c3e2-8b4d-4f9a-9c2e-7d6b5a4c3e1f");
            var menuAccessControlId = new Guid("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f");
            var menuRoleId = new Guid("c3d7e5f4-8d6f-4b9c-7e4a-5b8c6d7e4f3a");
            var menuUserId = new Guid("d4e8f6a5-7e7a-4c8d-6f5b-4a9d7c8e5f2b");

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { menuDashboardId, seedTime, null, "dashboard", "Dashboard", null, "/dashboard", 1, "Dashboard", null, null },
                    { menuAccessControlId, seedTime, null, "person", "AccessControl", null, null, 2, "Access Control", null, null },
                    { menuRoleId, seedTime, null, "badge", "Role", menuAccessControlId, "/access-control/role", 1, "Role Management", null, null },
                    { menuUserId, seedTime, null, "group", "User", menuAccessControlId, "/access-control/user", 2, "User Management", null, null }
                });

            // Seed data - Roles
            var superAdminRoleId = new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c");

            migrationBuilder.InsertData(
                table: "BackOfficeRoles",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Description", "IsSystem", "Name", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { superAdminRoleId, seedTime, null, "Full access to all BackOffice features", true, "SuperAdmin", null, null });

            // Seed data - Menu Actions
            var actionDashboardViewId = new Guid("e5f9a7b6-6f8b-4d9c-5e6a-3f8c7d9e0a1b");
            var actionRoleListId = new Guid("f6a8b7c5-5e9c-4a8d-4f7b-2e9d8c7f6a1c");
            var actionRoleAddId = new Guid("a7b9c8d6-4d8e-4f7a-3e8c-1d8f7c6b5a9d");
            var actionRoleEditId = new Guid("b8c9d7e5-3c7f-4e8b-2d9a-0c9e8d7f6b4c");
            var actionRoleDeleteId = new Guid("c9d8e7f4-2b6e-4d9c-1c8b-9f8e7d6c5b3a");
            var actionRoleViewId = new Guid("d8e7f6a3-1a5d-4c8e-0b9c-8e7f6d5c4b2f");
            var actionUserListId = new Guid("e7f6a5b2-0f4c-4b9d-9a8f-7e6d5c4b3a1e");
            var actionUserAddId = new Guid("f6a5b4c1-9e3b-4a8c-8f7e-6d5c4b3a2f0d");
            var actionUserEditId = new Guid("a5b4c3d2-8d2a-4f7b-7e6d-5c4b3a2f1e9c");
            var actionUserDeleteId = new Guid("b4c3d2e1-7c1f-4e8a-6d5c-4b3a2f1e0d8b");
            var actionUserViewId = new Guid("c3d2e1f0-6b0e-4d9f-5c4b-3a2f1e0d9c7a");

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { actionDashboardViewId, "View", seedTime, null, "View Dashboard", true, "Dashboard", null, null },
                    { actionRoleListId, "List", seedTime, null, "List Roles", true, "Role", null, null },
                    { actionRoleAddId, "Add", seedTime, null, "Add Role", true, "Role", null, null },
                    { actionRoleEditId, "Edit", seedTime, null, "Edit Role", true, "Role", null, null },
                    { actionRoleDeleteId, "Delete", seedTime, null, "Delete Role", true, "Role", null, null },
                    { actionRoleViewId, "View", seedTime, null, "View Role Details", true, "Role", null, null },
                    { actionUserListId, "List", seedTime, null, "List Users", true, "User", null, null },
                    { actionUserAddId, "Add", seedTime, null, "Add User", true, "User", null, null },
                    { actionUserEditId, "Edit", seedTime, null, "Edit User", true, "User", null, null },
                    { actionUserDeleteId, "Delete", seedTime, null, "Delete User", true, "User", null, null },
                    { actionUserViewId, "View", seedTime, null, "View User Details", true, "User", null, null }
                });

            // Seed data - SuperAdmin User
            var superAdminUserId = new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

            migrationBuilder.InsertData(
                table: "BackOfficeUsers",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Email", "FullName", "IsActive", "LastLoginAtUtc", "PasswordHash", "UpdatedAtUtc", "UpdatedBy", "UserName" },
                values: new object[] { superAdminUserId, seedTime, null, "superadmin@gmail.com", "Super Admin", true, null, "100000.EBESExQVFhcYGRobHB0eHw==.u3Fyc5ZjygvMHMkQlV122zvEFBsQzs5gNbr5qrTujjk=", null, null, "SuperAdmin" });

            // Seed data - User Roles (SuperAdmin user -> SuperAdmin role)
            var userRoleId = new Guid("a2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d");

            migrationBuilder.InsertData(
                table: "BackOfficeUserRoles",
                columns: new[] { "UserId", "RoleId", "CreatedAtUtc", "CreatedBy", "Id", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { superAdminUserId, superAdminRoleId, seedTime, null, userRoleId, null, null });

            // NOTE: BackOfficeRoleMenuActions are NOT seeded here.
            // They are assigned dynamically by SuperAdminPermissionSync hosted service on startup.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackOfficeRoleMenuActions");

            migrationBuilder.DropTable(
                name: "BackOfficeUserRoles");

            migrationBuilder.DropTable(
                name: "BackOfficeMenuActions");

            migrationBuilder.DropTable(
                name: "BackOfficeUsers");

            migrationBuilder.DropTable(
                name: "BackOfficeRoles");

            migrationBuilder.DropTable(
                name: "BackOfficeMenus");
        }
    }
}
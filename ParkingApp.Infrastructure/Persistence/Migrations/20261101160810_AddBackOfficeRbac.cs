using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("11111111-2222-3333-4444-555555555555"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("22222222-3333-4444-5555-666666666666"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("33333333-4444-5555-6666-777777777777"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("44444444-5555-6666-7777-888888888888"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("55555555-6666-7777-8888-999999999999"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeRoleMenuActions",
                keyColumns: new[] { "MenuActionId", "RoleId" },
                keyValues: new object[] { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.DeleteData(
                table: "BackOfficeUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301") });

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-2222-3333-4444-555555555555"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-666666666666"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-4444-5555-6666-777777777777"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-5555-6666-7777-888888888888"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("55555555-6666-7777-8888-999999999999"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            migrationBuilder.DeleteData(
                table: "BackOfficeRoles",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("a1f5c3e2-8b4d-4f9a-9c2e-7d6b5a4c3e1f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "dashboard", "Dashboard", null, "/dashboard", 1, "Dashboard", null, null },
                    { new Guid("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "person", "AccessControl", null, null, 2, "Access Control", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeRoles",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Description", "IsSystem", "Name", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Full access to all BackOffice features", true, "SuperAdmin", null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("e5f9a7b6-6f8b-4d9c-5e6a-3f8c7d9e0a1b"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Dashboard", true, "Dashboard", null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("c3d7e5f4-8d6f-4b9c-7e4a-5b8c6d7e4f3a"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "badge", "Role", new Guid("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f"), "/access-control/role", 1, "Role Management", null, null },
                    { new Guid("d4e8f6a5-7e7a-4c8d-6f5b-4a9d7c8e5f2b"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "group", "User", new Guid("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f"), "/access-control/user", 2, "User Management", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeUserRoles",
                columns: new[] { "RoleId", "UserId", "CreatedAtUtc", "CreatedBy", "Id", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c"), new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("a2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d"), null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("a5b4c3d2-8d2a-4f7b-7e6d-5c4b3a2f1e9c"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit User", true, "User", null, null },
                    { new Guid("a7b9c8d6-4d8e-4f7a-3e8c-1d8f7c6b5a9d"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Role", true, "Role", null, null },
                    { new Guid("b4c3d2e1-7c1f-4e8a-6d5c-4b3a2f1e0d8b"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete User", true, "User", null, null },
                    { new Guid("b8c9d7e5-3c7f-4e8b-2d9a-0c9e8d7f6b4c"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Role", true, "Role", null, null },
                    { new Guid("c3d2e1f0-6b0e-4d9f-5c4b-3a2f1e0d9c7a"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View User Details", true, "User", null, null },
                    { new Guid("c9d8e7f4-2b6e-4d9c-1c8b-9f8e7d6c5b3a"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Role", true, "Role", null, null },
                    { new Guid("d8e7f6a3-1a5d-4c8e-0b9c-8e7f6d5c4b2f"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Role Details", true, "Role", null, null },
                    { new Guid("e7f6a5b2-0f4c-4b9d-9a8f-7e6d5c4b3a1e"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Users", true, "User", null, null },
                    { new Guid("f6a5b4c1-9e3b-4a8c-8f7e-6d5c4b3a2f0d"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add User", true, "User", null, null },
                    { new Guid("f6a8b7c5-5e9c-4a8d-4f7b-2e9d8c7f6a1c"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Roles", true, "Role", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BackOfficeUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c"), new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301") });

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("a5b4c3d2-8d2a-4f7b-7e6d-5c4b3a2f1e9c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("a7b9c8d6-4d8e-4f7a-3e8c-1d8f7c6b5a9d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("b4c3d2e1-7c1f-4e8a-6d5c-4b3a2f1e0d8b"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("b8c9d7e5-3c7f-4e8b-2d9a-0c9e8d7f6b4c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("c3d2e1f0-6b0e-4d9f-5c4b-3a2f1e0d9c7a"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("c9d8e7f4-2b6e-4d9c-1c8b-9f8e7d6c5b3a"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("d8e7f6a3-1a5d-4c8e-0b9c-8e7f6d5c4b2f"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("e5f9a7b6-6f8b-4d9c-5e6a-3f8c7d9e0a1b"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("e7f6a5b2-0f4c-4b9d-9a8f-7e6d5c4b3a1e"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("f6a5b4c1-9e3b-4a8c-8f7e-6d5c4b3a2f0d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("f6a8b7c5-5e9c-4a8d-4f7b-2e9d8c7f6a1c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeRoles",
                keyColumn: "Id",
                keyValue: new Guid("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("a1f5c3e2-8b4d-4f9a-9c2e-7d6b5a4c3e1f"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("c3d7e5f4-8d6f-4b9c-7e4a-5b8c6d7e4f3a"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("d4e8f6a5-7e7a-4c8d-6f5b-4a9d7c8e5f2b"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f"));

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "dashboard", "Dashboard", null, "/dashboard", 1, "Dashboard", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "person", "AccessControl", null, null, 2, "Access Control", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeRoles",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Description", "IsSystem", "Name", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Full access to all BackOffice features", true, "SuperAdmin", null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Dashboard", true, "Dashboard", null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "badge", "Role", new Guid("22222222-2222-2222-2222-222222222222"), "/access-control/role", 1, "Role Management", null, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "group", "User", new Guid("22222222-2222-2222-2222-222222222222"), "/access-control/user", 2, "User Management", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeUserRoles",
                columns: new[] { "RoleId", "UserId", "CreatedAtUtc", "CreatedBy", "Id", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("3a8a8c52-6b8d-49e8-ba87-107bc8c283fa"), null, null });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-2222-3333-4444-555555555555"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Users", true, "User", null, null },
                    { new Guid("22222222-3333-4444-5555-666666666666"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add User", true, "User", null, null },
                    { new Guid("33333333-4444-5555-6666-777777777777"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit User", true, "User", null, null },
                    { new Guid("44444444-5555-6666-7777-888888888888"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete User", true, "User", null, null },
                    { new Guid("55555555-6666-7777-8888-999999999999"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View User Details", true, "User", null, null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Roles", true, "Role", null, null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Role", true, "Role", null, null },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Role", true, "Role", null, null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Role", true, "Role", null, null },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Role Details", true, "Role", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeRoleMenuActions",
                columns: new[] { "MenuActionId", "RoleId", "CreatedAtUtc", "CreatedBy", "Id", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("11111111-1111-1111-1111-111111111111"), null, null },
                    { new Guid("11111111-2222-3333-4444-555555555555"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("77777777-7777-7777-7777-777777777778"), null, null },
                    { new Guid("22222222-3333-4444-5555-666666666666"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("88888888-8888-8888-8888-888888888888"), null, null },
                    { new Guid("33333333-4444-5555-6666-777777777777"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("99999999-9999-9999-9999-999999999999"), null, null },
                    { new Guid("44444444-5555-6666-7777-888888888888"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"), null, null },
                    { new Guid("55555555-6666-7777-8888-999999999999"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc"), null, null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("22222222-2222-2222-2222-222222222222"), null, null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("33333333-3333-3333-3333-333333333333"), null, null },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-4444-4444-4444-444444444444"), null, null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("55555555-5555-5555-5555-555555555555"), null, null },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("66666666-6666-6666-6666-666666666666"), null, null }
                });
        }
    }
}

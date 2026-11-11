using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ParkingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeMenuHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("09e5bca8-8f71-4d30-acb4-5b6d7e8f9a0c"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "motorcycle", "RidersManagement", null, null, 5, "Riders Management", null, null },
                    { new Guid("1af6cdb9-9082-4e41-bdc5-6c7e8f9a0b1d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "parking", "FreeParkings", null, "/free-parkings", 6, "Free Parkings", null, null },
                    { new Guid("e7c39a86-6d5f-4b1e-8a92-3f4b5c6d7e8a"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "staff", "StaffManagement", null, "/staff", 3, "Staff Management", null, null },
                    { new Guid("f8d4ab97-7e60-4c2f-9ba3-4a5c6d7e8f9b"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "building", "PaidParkings", null, null, 4, "Paid Parkings", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("2a7f4c91-8b3d-4e6a-9f52-1c7d0e4b8a63"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Staff", true, "StaffManagement", null, null },
                    { new Guid("3b8e5da2-9c4e-4f7b-a063-2d8e1f5c9b74"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Staff Details", true, "StaffManagement", null, null },
                    { new Guid("4c9f6eb3-ad5f-408c-b174-3e9f206ad85c"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Staff", true, "StaffManagement", null, null },
                    { new Guid("5da01ec5-cf71-42ae-d396-50b1428cf9a6"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Free Parkings", true, "FreeParkings", null, null },
                    { new Guid("5da07fc4-be60-419d-c285-4fa0317be96d"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Staff", true, "StaffManagement", null, null },
                    { new Guid("6eb12fd6-d082-43bf-e4a7-61c2539d0a97"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Riders Management", true, "RidersManagement", null, null },
                    { new Guid("6eb180d5-cf71-42ae-d396-50b1428cf9a7"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Staff", true, "StaffManagement", null, null },
                    { new Guid("7fc230e7-e193-44c0-f5b8-72d364ae1ba8"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Riders Management", true, "RidersManagement", null, null },
                    { new Guid("7fc291e6-d082-43bf-e4a7-61c2539d0ab8"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Paid Parkings", true, "PaidParkings", null, null },
                    { new Guid("80d341f8-f2a4-45d1-06c9-83e475bf2cb9"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Riders Management", true, "RidersManagement", null, null },
                    { new Guid("80d3a2f7-e193-44c0-f5b8-72d364ae1bc9"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Paid Parkings", true, "PaidParkings", null, null },
                    { new Guid("91e45209-03b5-46e2-17da-94f586c03dca"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Riders Management", true, "RidersManagement", null, null },
                    { new Guid("91e4b308-f2a4-45d1-06c9-83e475bf2cda"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Paid Parking", true, "PaidParkings", null, null },
                    { new Guid("a2f5631a-14c6-47f3-28eb-a50697d14edb"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Riders Management", true, "RidersManagement", null, null },
                    { new Guid("a2f5631b-25d7-4804-39fc-b617a8e25f0b"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Free Parkings", true, "FreeParkings", null, null },
                    { new Guid("a2f5c419-03b5-46e2-17da-94f586c03deb"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Paid Parking", true, "PaidParkings", null, null },
                    { new Guid("b306742c-36e8-4915-4a0d-c728b9f3601c"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Free Parking", true, "FreeParkings", null, null },
                    { new Guid("b306d52a-14c6-47f3-28eb-a50697d14efc"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Paid Parking", true, "PaidParkings", null, null },
                    { new Guid("c417853d-47f9-4a26-5b1e-d839ca04712d"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Free Parking", true, "FreeParkings", null, null },
                    { new Guid("d528964e-580a-4b37-6c2f-e94adb15823e"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Free Parking", true, "FreeParkings", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeMenus",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Icon", "Key", "ParentMenuId", "Route", "SortOrder", "Title", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("2b07deca-a193-4f52-ced6-7d8f9a0b1c2e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "building", "Facilities", new Guid("f8d4ab97-7e60-4c2f-9ba3-4a5c6d7e8f9b"), "/paid-parkings/facilities", 1, "Paid Parking Facility", null, null },
                    { new Guid("3c18efdb-b2a4-4063-dfe7-8e9a0b1c2d3f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "users", "Providers", new Guid("f8d4ab97-7e60-4c2f-9ba3-4a5c6d7e8f9b"), "/paid-parkings/providers", 2, "Parking Facility Providers", null, null },
                    { new Guid("4d29f0ec-c3b5-4174-e0f8-9fab1c2d3e40"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "file", "Licenses", new Guid("09e5bca8-8f71-4d30-acb4-5b6d7e8f9a0c"), "/riders/licenses", 1, "Licenses", null, null },
                    { new Guid("5e3a01fd-d4c6-4285-f109-ab0c1d2e3f51"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "motorcycle", "Riders", new Guid("09e5bca8-8f71-4d30-acb4-5b6d7e8f9a0c"), "/riders/riders", 2, "Riders", null, null }
                });

            migrationBuilder.InsertData(
                table: "BackOfficeMenuActions",
                columns: new[] { "Id", "ActionKey", "CreatedAtUtc", "CreatedBy", "DisplayName", "IsActive", "MenuKey", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("085bc970-7a2c-4d59-8e41-0b6cfd37a451"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Riders", true, "Riders", null, null },
                    { new Guid("085bca7f-691b-4c48-7d30-fa5bec269341"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Parking Facility", true, "Facilities", null, null },
                    { new Guid("196cda81-8b3d-4e6a-9f52-1c7d0e48b562"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Rider Details", true, "Riders", null, null },
                    { new Guid("196cdb80-7a2c-4d59-8e41-0b6cfd37a452"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Parking Providers", true, "Providers", null, null },
                    { new Guid("2a7deb92-9c4e-4f7b-a063-2d8e1f59c673"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Rider", true, "Riders", null, null },
                    { new Guid("2a7dec91-8b3d-4e6a-9f52-1c7d0e48b563"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Parking Provider Details", true, "Providers", null, null },
                    { new Guid("3b8efca3-ad5f-408c-b174-3e9f206ad784"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Rider", true, "Riders", null, null },
                    { new Guid("3b8efda2-9c4e-4f7b-a063-2d8e1f59c674"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Parking Provider", true, "Providers", null, null },
                    { new Guid("4c9f0db4-be60-419d-c285-4fa0317be895"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Rider", true, "Riders", null, null },
                    { new Guid("4c9f0eb3-ad5f-408c-b174-3e9f206ad785"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Parking Provider", true, "Providers", null, null },
                    { new Guid("5da01fc4-be60-419d-c285-4fa0317be896"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete Parking Provider", true, "Providers", null, null },
                    { new Guid("b306742b-25d7-4804-39fc-b617a8e25f0c"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Licenses", true, "Licenses", null, null },
                    { new Guid("c417853c-36e8-4915-4a0d-c728b9f3601d"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View License Details", true, "Licenses", null, null },
                    { new Guid("c417e63b-25d7-4804-39fc-b617a8e25f0d"), "List", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "List Parking Facilities", true, "Facilities", null, null },
                    { new Guid("d528964d-47f9-4a26-5b1e-d839ca04712e"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add License", true, "Licenses", null, null },
                    { new Guid("d528f74c-36e8-4915-4a0d-c728b9f3601e"), "View", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "View Parking Facility Details", true, "Facilities", null, null },
                    { new Guid("e639a75e-580a-4b37-6c2f-e94adb15823f"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit License", true, "Licenses", null, null },
                    { new Guid("e639a85d-47f9-4a26-5b1e-d839ca04712f"), "Add", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Add Parking Facility", true, "Facilities", null, null },
                    { new Guid("f74ab86f-691b-4c48-7d30-fa5bec269340"), "Delete", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Delete License", true, "Licenses", null, null },
                    { new Guid("f74ab96e-580a-4b37-6c2f-e94adb158230"), "Edit", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Edit Parking Facility", true, "Facilities", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("085bc970-7a2c-4d59-8e41-0b6cfd37a451"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("085bca7f-691b-4c48-7d30-fa5bec269341"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("196cda81-8b3d-4e6a-9f52-1c7d0e48b562"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("196cdb80-7a2c-4d59-8e41-0b6cfd37a452"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("2a7deb92-9c4e-4f7b-a063-2d8e1f59c673"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("2a7dec91-8b3d-4e6a-9f52-1c7d0e48b563"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("2a7f4c91-8b3d-4e6a-9f52-1c7d0e4b8a63"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("3b8e5da2-9c4e-4f7b-a063-2d8e1f5c9b74"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("3b8efca3-ad5f-408c-b174-3e9f206ad784"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("3b8efda2-9c4e-4f7b-a063-2d8e1f59c674"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("4c9f0db4-be60-419d-c285-4fa0317be895"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("4c9f0eb3-ad5f-408c-b174-3e9f206ad785"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("4c9f6eb3-ad5f-408c-b174-3e9f206ad85c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("5da01ec5-cf71-42ae-d396-50b1428cf9a6"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("5da01fc4-be60-419d-c285-4fa0317be896"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("5da07fc4-be60-419d-c285-4fa0317be96d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("6eb12fd6-d082-43bf-e4a7-61c2539d0a97"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("6eb180d5-cf71-42ae-d396-50b1428cf9a7"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("7fc230e7-e193-44c0-f5b8-72d364ae1ba8"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("7fc291e6-d082-43bf-e4a7-61c2539d0ab8"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("80d341f8-f2a4-45d1-06c9-83e475bf2cb9"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("80d3a2f7-e193-44c0-f5b8-72d364ae1bc9"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("91e45209-03b5-46e2-17da-94f586c03dca"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("91e4b308-f2a4-45d1-06c9-83e475bf2cda"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("a2f5631a-14c6-47f3-28eb-a50697d14edb"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("a2f5631b-25d7-4804-39fc-b617a8e25f0b"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("a2f5c419-03b5-46e2-17da-94f586c03deb"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("b306742b-25d7-4804-39fc-b617a8e25f0c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("b306742c-36e8-4915-4a0d-c728b9f3601c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("b306d52a-14c6-47f3-28eb-a50697d14efc"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("c417853c-36e8-4915-4a0d-c728b9f3601d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("c417853d-47f9-4a26-5b1e-d839ca04712d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("c417e63b-25d7-4804-39fc-b617a8e25f0d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("d528964d-47f9-4a26-5b1e-d839ca04712e"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("d528964e-580a-4b37-6c2f-e94adb15823e"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("d528f74c-36e8-4915-4a0d-c728b9f3601e"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("e639a75e-580a-4b37-6c2f-e94adb15823f"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("e639a85d-47f9-4a26-5b1e-d839ca04712f"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("f74ab86f-691b-4c48-7d30-fa5bec269340"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenuActions",
                keyColumn: "Id",
                keyValue: new Guid("f74ab96e-580a-4b37-6c2f-e94adb158230"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("1af6cdb9-9082-4e41-bdc5-6c7e8f9a0b1d"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("2b07deca-a193-4f52-ced6-7d8f9a0b1c2e"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("3c18efdb-b2a4-4063-dfe7-8e9a0b1c2d3f"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("4d29f0ec-c3b5-4174-e0f8-9fab1c2d3e40"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("5e3a01fd-d4c6-4285-f109-ab0c1d2e3f51"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("e7c39a86-6d5f-4b1e-8a92-3f4b5c6d7e8a"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("09e5bca8-8f71-4d30-acb4-5b6d7e8f9a0c"));

            migrationBuilder.DeleteData(
                table: "BackOfficeMenus",
                keyColumn: "Id",
                keyValue: new Guid("f8d4ab97-7e60-4c2f-9ba3-4a5c6d7e8f9b"));
        }
    }
}

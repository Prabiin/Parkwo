using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Menu;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeMenuActionConfiguration : IEntityTypeConfiguration<BackOfficeMenuAction>
{
    // Random GUIDs for seed data
    private static readonly Guid ActionDashboardViewId = new("e5f9a7b6-6f8b-4d9c-5e6a-3f8c7d9e0a1b");
    private static readonly Guid ActionRoleListId = new("f6a8b7c5-5e9c-4a8d-4f7b-2e9d8c7f6a1c");
    private static readonly Guid ActionRoleAddId = new("a7b9c8d6-4d8e-4f7a-3e8c-1d8f7c6b5a9d");
    private static readonly Guid ActionRoleEditId = new("b8c9d7e5-3c7f-4e8b-2d9a-0c9e8d7f6b4c");
    private static readonly Guid ActionRoleDeleteId = new("c9d8e7f4-2b6e-4d9c-1c8b-9f8e7d6c5b3a");
    private static readonly Guid ActionRoleViewId = new("d8e7f6a3-1a5d-4c8e-0b9c-8e7f6d5c4b2f");
    private static readonly Guid ActionUserListId = new("e7f6a5b2-0f4c-4b9d-9a8f-7e6d5c4b3a1e");
    private static readonly Guid ActionUserAddId = new("f6a5b4c1-9e3b-4a8c-8f7e-6d5c4b3a2f0d");
    private static readonly Guid ActionUserEditId = new("a5b4c3d2-8d2a-4f7b-7e6d-5c4b3a2f1e9c");
    private static readonly Guid ActionUserDeleteId = new("b4c3d2e1-7c1f-4e8a-6d5c-4b3a2f1e0d8b");
    private static readonly Guid ActionUserViewId = new("c3d2e1f0-6b0e-4d9f-5c4b-3a2f1e0d9c7a");

    // Actions added after the initial RBAC rollout. One set of the five plain
    // actions (List, View, Add, Edit, Delete) per new menu. Uniqueness is on
    // (MenuKey, ActionKey), so the same ActionKey under a different MenuKey is
    // a different row and never collides.
    private static readonly Guid ActionStaffListId = new("2a7f4c91-8b3d-4e6a-9f52-1c7d0e4b8a63");
    private static readonly Guid ActionStaffViewId = new("3b8e5da2-9c4e-4f7b-a063-2d8e1f5c9b74");
    private static readonly Guid ActionStaffAddId = new("4c9f6eb3-ad5f-408c-b174-3e9f206ad85c");
    private static readonly Guid ActionStaffEditId = new("5da07fc4-be60-419d-c285-4fa0317be96d");
    private static readonly Guid ActionStaffDeleteId = new("6eb180d5-cf71-42ae-d396-50b1428cf9a7");

    private static readonly Guid ActionPaidParkingsListId = new("7fc291e6-d082-43bf-e4a7-61c2539d0ab8");
    private static readonly Guid ActionPaidParkingsViewId = new("80d3a2f7-e193-44c0-f5b8-72d364ae1bc9");
    private static readonly Guid ActionPaidParkingsAddId = new("91e4b308-f2a4-45d1-06c9-83e475bf2cda");
    private static readonly Guid ActionPaidParkingsEditId = new("a2f5c419-03b5-46e2-17da-94f586c03deb");
    private static readonly Guid ActionPaidParkingsDeleteId = new("b306d52a-14c6-47f3-28eb-a50697d14efc");

    private static readonly Guid ActionFacilitiesListId = new("c417e63b-25d7-4804-39fc-b617a8e25f0d");
    private static readonly Guid ActionFacilitiesViewId = new("d528f74c-36e8-4915-4a0d-c728b9f3601e");
    private static readonly Guid ActionFacilitiesAddId = new("e639a85d-47f9-4a26-5b1e-d839ca04712f");
    private static readonly Guid ActionFacilitiesEditId = new("f74ab96e-580a-4b37-6c2f-e94adb158230");
    private static readonly Guid ActionFacilitiesDeleteId = new("085bca7f-691b-4c48-7d30-fa5bec269341");

    private static readonly Guid ActionProvidersListId = new("196cdb80-7a2c-4d59-8e41-0b6cfd37a452");
    private static readonly Guid ActionProvidersViewId = new("2a7dec91-8b3d-4e6a-9f52-1c7d0e48b563");
    private static readonly Guid ActionProvidersAddId = new("3b8efda2-9c4e-4f7b-a063-2d8e1f59c674");
    private static readonly Guid ActionProvidersEditId = new("4c9f0eb3-ad5f-408c-b174-3e9f206ad785");
    private static readonly Guid ActionProvidersDeleteId = new("5da01fc4-be60-419d-c285-4fa0317be896");

    private static readonly Guid ActionRidersManagementListId = new("6eb12fd6-d082-43bf-e4a7-61c2539d0a97");
    private static readonly Guid ActionRidersManagementViewId = new("7fc230e7-e193-44c0-f5b8-72d364ae1ba8");
    private static readonly Guid ActionRidersManagementAddId = new("80d341f8-f2a4-45d1-06c9-83e475bf2cb9");
    private static readonly Guid ActionRidersManagementEditId = new("91e45209-03b5-46e2-17da-94f586c03dca");
    private static readonly Guid ActionRidersManagementDeleteId = new("a2f5631a-14c6-47f3-28eb-a50697d14edb");

    private static readonly Guid ActionLicensesListId = new("b306742b-25d7-4804-39fc-b617a8e25f0c");
    private static readonly Guid ActionLicensesViewId = new("c417853c-36e8-4915-4a0d-c728b9f3601d");
    private static readonly Guid ActionLicensesAddId = new("d528964d-47f9-4a26-5b1e-d839ca04712e");
    private static readonly Guid ActionLicensesEditId = new("e639a75e-580a-4b37-6c2f-e94adb15823f");
    private static readonly Guid ActionLicensesDeleteId = new("f74ab86f-691b-4c48-7d30-fa5bec269340");

    private static readonly Guid ActionRidersListId = new("085bc970-7a2c-4d59-8e41-0b6cfd37a451");
    private static readonly Guid ActionRidersViewId = new("196cda81-8b3d-4e6a-9f52-1c7d0e48b562");
    private static readonly Guid ActionRidersAddId = new("2a7deb92-9c4e-4f7b-a063-2d8e1f59c673");
    private static readonly Guid ActionRidersEditId = new("3b8efca3-ad5f-408c-b174-3e9f206ad784");
    private static readonly Guid ActionRidersDeleteId = new("4c9f0db4-be60-419d-c285-4fa0317be895");

    private static readonly Guid ActionFreeParkingsListId = new("5da01ec5-cf71-42ae-d396-50b1428cf9a6");
    private static readonly Guid ActionFreeParkingsViewId = new("a2f5631b-25d7-4804-39fc-b617a8e25f0b");
    private static readonly Guid ActionFreeParkingsAddId = new("b306742c-36e8-4915-4a0d-c728b9f3601c");
    private static readonly Guid ActionFreeParkingsEditId = new("c417853d-47f9-4a26-5b1e-d839ca04712d");
    private static readonly Guid ActionFreeParkingsDeleteId = new("d528964e-580a-4b37-6c2f-e94adb15823e");

    private static readonly DateTimeOffset SeedTime = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BackOfficeMenuAction> builder)
    {
        builder.ToTable("BackOfficeMenuActions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MenuKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ActionKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.MenuKey, x.ActionKey })
            .IsUnique();

        builder.HasOne(x => x.Menu)
            .WithMany(x => x.Actions)
            .HasForeignKey(x => x.MenuKey)
            .HasPrincipalKey(x => x.Key)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoleMenuActions)
            .WithOne(x => x.MenuAction)
            .HasForeignKey(x => x.MenuActionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            new BackOfficeMenuAction
            {
                Id = ActionDashboardViewId,
                MenuKey = "Dashboard",
                ActionKey = "View",
                DisplayName = "View Dashboard",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRoleListId,
                MenuKey = "Role",
                ActionKey = "List",
                DisplayName = "List Roles",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRoleAddId,
                MenuKey = "Role",
                ActionKey = "Add",
                DisplayName = "Add Role",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRoleEditId,
                MenuKey = "Role",
                ActionKey = "Edit",
                DisplayName = "Edit Role",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRoleDeleteId,
                MenuKey = "Role",
                ActionKey = "Delete",
                DisplayName = "Delete Role",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRoleViewId,
                MenuKey = "Role",
                ActionKey = "View",
                DisplayName = "View Role Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionUserListId,
                MenuKey = "User",
                ActionKey = "List",
                DisplayName = "List Users",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionUserAddId,
                MenuKey = "User",
                ActionKey = "Add",
                DisplayName = "Add User",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionUserEditId,
                MenuKey = "User",
                ActionKey = "Edit",
                DisplayName = "Edit User",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionUserDeleteId,
                MenuKey = "User",
                ActionKey = "Delete",
                DisplayName = "Delete User",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionUserViewId,
                MenuKey = "User",
                ActionKey = "View",
                DisplayName = "View User Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionStaffListId,
                MenuKey = "StaffManagement",
                ActionKey = "List",
                DisplayName = "List Staff",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionStaffViewId,
                MenuKey = "StaffManagement",
                ActionKey = "View",
                DisplayName = "View Staff Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionStaffAddId,
                MenuKey = "StaffManagement",
                ActionKey = "Add",
                DisplayName = "Add Staff",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionStaffEditId,
                MenuKey = "StaffManagement",
                ActionKey = "Edit",
                DisplayName = "Edit Staff",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionStaffDeleteId,
                MenuKey = "StaffManagement",
                ActionKey = "Delete",
                DisplayName = "Delete Staff",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionPaidParkingsListId,
                MenuKey = "PaidParkings",
                ActionKey = "List",
                DisplayName = "List Paid Parkings",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionPaidParkingsViewId,
                MenuKey = "PaidParkings",
                ActionKey = "View",
                DisplayName = "View Paid Parkings",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionPaidParkingsAddId,
                MenuKey = "PaidParkings",
                ActionKey = "Add",
                DisplayName = "Add Paid Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionPaidParkingsEditId,
                MenuKey = "PaidParkings",
                ActionKey = "Edit",
                DisplayName = "Edit Paid Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionPaidParkingsDeleteId,
                MenuKey = "PaidParkings",
                ActionKey = "Delete",
                DisplayName = "Delete Paid Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFacilitiesListId,
                MenuKey = "Facilities",
                ActionKey = "List",
                DisplayName = "List Parking Facilities",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFacilitiesViewId,
                MenuKey = "Facilities",
                ActionKey = "View",
                DisplayName = "View Parking Facility Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFacilitiesAddId,
                MenuKey = "Facilities",
                ActionKey = "Add",
                DisplayName = "Add Parking Facility",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFacilitiesEditId,
                MenuKey = "Facilities",
                ActionKey = "Edit",
                DisplayName = "Edit Parking Facility",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFacilitiesDeleteId,
                MenuKey = "Facilities",
                ActionKey = "Delete",
                DisplayName = "Delete Parking Facility",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionProvidersListId,
                MenuKey = "Providers",
                ActionKey = "List",
                DisplayName = "List Parking Providers",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionProvidersViewId,
                MenuKey = "Providers",
                ActionKey = "View",
                DisplayName = "View Parking Provider Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionProvidersAddId,
                MenuKey = "Providers",
                ActionKey = "Add",
                DisplayName = "Add Parking Provider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionProvidersEditId,
                MenuKey = "Providers",
                ActionKey = "Edit",
                DisplayName = "Edit Parking Provider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionProvidersDeleteId,
                MenuKey = "Providers",
                ActionKey = "Delete",
                DisplayName = "Delete Parking Provider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersManagementListId,
                MenuKey = "RidersManagement",
                ActionKey = "List",
                DisplayName = "List Riders Management",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersManagementViewId,
                MenuKey = "RidersManagement",
                ActionKey = "View",
                DisplayName = "View Riders Management",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersManagementAddId,
                MenuKey = "RidersManagement",
                ActionKey = "Add",
                DisplayName = "Add Riders Management",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersManagementEditId,
                MenuKey = "RidersManagement",
                ActionKey = "Edit",
                DisplayName = "Edit Riders Management",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersManagementDeleteId,
                MenuKey = "RidersManagement",
                ActionKey = "Delete",
                DisplayName = "Delete Riders Management",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionLicensesListId,
                MenuKey = "Licenses",
                ActionKey = "List",
                DisplayName = "List Licenses",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionLicensesViewId,
                MenuKey = "Licenses",
                ActionKey = "View",
                DisplayName = "View License Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionLicensesAddId,
                MenuKey = "Licenses",
                ActionKey = "Add",
                DisplayName = "Add License",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionLicensesEditId,
                MenuKey = "Licenses",
                ActionKey = "Edit",
                DisplayName = "Edit License",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionLicensesDeleteId,
                MenuKey = "Licenses",
                ActionKey = "Delete",
                DisplayName = "Delete License",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersListId,
                MenuKey = "Riders",
                ActionKey = "List",
                DisplayName = "List Riders",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersViewId,
                MenuKey = "Riders",
                ActionKey = "View",
                DisplayName = "View Rider Details",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersAddId,
                MenuKey = "Riders",
                ActionKey = "Add",
                DisplayName = "Add Rider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersEditId,
                MenuKey = "Riders",
                ActionKey = "Edit",
                DisplayName = "Edit Rider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionRidersDeleteId,
                MenuKey = "Riders",
                ActionKey = "Delete",
                DisplayName = "Delete Rider",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFreeParkingsListId,
                MenuKey = "FreeParkings",
                ActionKey = "List",
                DisplayName = "List Free Parkings",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFreeParkingsViewId,
                MenuKey = "FreeParkings",
                ActionKey = "View",
                DisplayName = "View Free Parkings",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFreeParkingsAddId,
                MenuKey = "FreeParkings",
                ActionKey = "Add",
                DisplayName = "Add Free Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFreeParkingsEditId,
                MenuKey = "FreeParkings",
                ActionKey = "Edit",
                DisplayName = "Edit Free Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenuAction
            {
                Id = ActionFreeParkingsDeleteId,
                MenuKey = "FreeParkings",
                ActionKey = "Delete",
                DisplayName = "Delete Free Parking",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            });
    }
}
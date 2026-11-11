using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Menu;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeMenuConfiguration : IEntityTypeConfiguration<BackOfficeMenu>
{
    // Random GUIDs for seed data
    private static readonly Guid MenuDashboardId = new("a1f5c3e2-8b4d-4f9a-9c2e-7d6b5a4c3e1f");
    private static readonly Guid MenuAccessControlId = new("b2e6d4f3-9c5e-4a8b-8d3f-6c7a5b4e2d1f");
    private static readonly Guid MenuRoleId = new("c3d7e5f4-8d6f-4b9c-7e4a-5b8c6d7e4f3a");
    private static readonly Guid MenuUserId = new("d4e8f6a5-7e7a-4c8d-6f5b-4a9d7c8e5f2b");

    // Top-level menus added after the initial RBAC rollout
    private static readonly Guid MenuStaffManagementId = new("e7c39a86-6d5f-4b1e-8a92-3f4b5c6d7e8a");
    private static readonly Guid MenuPaidParkingsId = new("f8d4ab97-7e60-4c2f-9ba3-4a5c6d7e8f9b");
    private static readonly Guid MenuRidersManagementId = new("09e5bca8-8f71-4d30-acb4-5b6d7e8f9a0c");
    private static readonly Guid MenuFreeParkingsId = new("1af6cdb9-9082-4e41-bdc5-6c7e8f9a0b1d");

    // Child menus added after the initial RBAC rollout
    private static readonly Guid MenuFacilitiesId = new("2b07deca-a193-4f52-ced6-7d8f9a0b1c2e");
    private static readonly Guid MenuProvidersId = new("3c18efdb-b2a4-4063-dfe7-8e9a0b1c2d3f");
    private static readonly Guid MenuLicensesId = new("4d29f0ec-c3b5-4174-e0f8-9fab1c2d3e40");
    private static readonly Guid MenuRidersId = new("5e3a01fd-d4c6-4285-f109-ab0c1d2e3f51");

    private static readonly DateTimeOffset SeedTime = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BackOfficeMenu> builder)
    {
        builder.ToTable("BackOfficeMenus");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .HasMaxLength(500);

        builder.Property(x => x.Icon)
            .HasMaxLength(100);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.HasOne(x => x.ParentMenu)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentMenuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Actions)
            .WithOne(x => x.Menu)
            .HasForeignKey(x => x.MenuKey)
            .HasPrincipalKey(x => x.Key)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            new BackOfficeMenu
            {
                Id = MenuDashboardId,
                Key = "Dashboard",
                Title = "Dashboard",
                Route = "/dashboard",
                Icon = "dashboard",
                SortOrder = 1,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuAccessControlId,
                Key = "AccessControl",
                Title = "Access Control",
                Route = null,
                Icon = "person",
                SortOrder = 2,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuRoleId,
                Key = "Role",
                Title = "Role Management",
                Route = "/access-control/role",
                Icon = "badge",
                SortOrder = 1,
                ParentMenuId = MenuAccessControlId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuUserId,
                Key = "User",
                Title = "User Management",
                Route = "/access-control/user",
                Icon = "group",
                SortOrder = 2,
                ParentMenuId = MenuAccessControlId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuStaffManagementId,
                Key = "StaffManagement",
                Title = "Staff Management",
                Route = "/staff",
                Icon = "staff",
                SortOrder = 3,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuPaidParkingsId,
                Key = "PaidParkings",
                Title = "Paid Parkings",
                Route = null,
                Icon = "building",
                SortOrder = 4,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuRidersManagementId,
                Key = "RidersManagement",
                Title = "Riders Management",
                Route = null,
                Icon = "motorcycle",
                SortOrder = 5,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuFreeParkingsId,
                Key = "FreeParkings",
                Title = "Free Parkings",
                Route = "/free-parkings",
                Icon = "parking",
                SortOrder = 6,
                ParentMenuId = null,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuFacilitiesId,
                Key = "Facilities",
                Title = "Paid Parking Facility",
                Route = "/paid-parkings/facilities",
                Icon = "building",
                SortOrder = 1,
                ParentMenuId = MenuPaidParkingsId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuProvidersId,
                Key = "Providers",
                Title = "Parking Facility Providers",
                Route = "/paid-parkings/providers",
                Icon = "users",
                SortOrder = 2,
                ParentMenuId = MenuPaidParkingsId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuLicensesId,
                Key = "Licenses",
                Title = "Licenses",
                Route = "/riders/licenses",
                Icon = "file",
                SortOrder = 1,
                ParentMenuId = MenuRidersManagementId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            },
            new BackOfficeMenu
            {
                Id = MenuRidersId,
                Key = "Riders",
                Title = "Riders",
                Route = "/riders/riders",
                Icon = "motorcycle",
                SortOrder = 2,
                ParentMenuId = MenuRidersManagementId,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            });
    }
}
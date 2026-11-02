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
            });
    }
}
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
            });
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeUserRoleConfiguration : IEntityTypeConfiguration<BackOfficeUserRole>
{
    // SuperAdmin user ID (from migration 20261101142141_UpdateSuperAdminUserId)
    private static readonly Guid SuperAdminUserId = new("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
    // SuperAdmin role ID (from BackOfficeRoleConfiguration)
    private static readonly Guid SuperAdminRoleId = new("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c");
    private static readonly DateTimeOffset SeedTime = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BackOfficeUserRole> builder)
    {
        builder.ToTable("BackOfficeUserRoles");

        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data: Link SuperAdmin user to SuperAdmin role
        builder.HasData(
            new BackOfficeUserRole
            {
                UserId = SuperAdminUserId,
                RoleId = SuperAdminRoleId,
                Id = new Guid("a2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d"),
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            });
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeRoleMenuActionConfiguration : IEntityTypeConfiguration<BackOfficeRoleMenuAction>
{
    public void Configure(EntityTypeBuilder<BackOfficeRoleMenuAction> builder)
    {
        builder.ToTable("BackOfficeRoleMenuActions");

        builder.HasKey(x => new { x.RoleId, x.MenuActionId });

        builder.HasOne(x => x.Role)
            .WithMany(x => x.RoleMenuActions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MenuAction)
            .WithMany(x => x.RoleMenuActions)
            .HasForeignKey(x => x.MenuActionId)
            .OnDelete(DeleteBehavior.Cascade);

        // NOTE: SuperAdmin role-action links are seeded via SQL in migrations
        // (see migration 20261101160810_AddBackOfficeRbac and future migrations)
        // This avoids maintaining HasData in sync with MenuActionConfiguration.
    }
}
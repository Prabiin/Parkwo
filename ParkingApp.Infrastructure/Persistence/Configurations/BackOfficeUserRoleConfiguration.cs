using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeUserRoleConfiguration : IEntityTypeConfiguration<BackOfficeUserRole>
{
    public void Configure(EntityTypeBuilder<BackOfficeUserRole> builder)
    {
        builder.ToTable("BackOfficeUserRoles");

        // 1:1 relationship - UserId is the primary key (enforces one role per user)
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne(x => x.UserRole)
            .HasForeignKey<BackOfficeUserRole>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on RoleId for queries
        builder.HasIndex(x => x.RoleId);
    }
}
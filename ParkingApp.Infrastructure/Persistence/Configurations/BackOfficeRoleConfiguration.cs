using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeRoleConfiguration : IEntityTypeConfiguration<BackOfficeRole>
{
    // Random GUID for SuperAdmin role
    private static readonly Guid SuperAdminRoleId = new("f1e2d3c4-b5a6-4f9e-8d7c-6b5a4f3e2d1c");
    private static readonly DateTimeOffset SeedTime = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BackOfficeRole> builder)
    {
        builder.ToTable("BackOfficeRoles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique among roles that have not been deleted. A deleted role keeps
        // its row, but it releases its name so the role can be recreated; an
        // inactive role still holds its name, because it can come back.
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(x => x.UserRoles)
            .WithOne(x => x.Role)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoleMenuActions)
            .WithOne(x => x.Role)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data: SuperAdmin system role
        builder.HasData(
            new BackOfficeRole
            {
                Id = SuperAdminRoleId,
                Name = "SuperAdmin",
                Description = "Full access to all BackOffice features",
                IsSystem = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            });
    }
}
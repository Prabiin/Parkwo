using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeUserConfiguration : IEntityTypeConfiguration<BackOfficeUser>
{
    // SuperAdmin user ID (matches migration 20261101142141_UpdateSuperAdminUserId)
    private static readonly Guid SuperAdminUserId = new("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
    private static readonly DateTimeOffset SeedTime = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BackOfficeUser> builder)
    {
        builder.ToTable("BackOfficeUsers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.LastLoginAtUtc);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        // Seed data: SuperAdmin user
        builder.HasData(
            new BackOfficeUser
            {
                Id = SuperAdminUserId,
                UserName = "SuperAdmin",
                FullName = "Super Admin",
                Email = "superadmin@gmail.com",
                // PBKDF2 hash of "P@ssw0rd" (iterations.salt.hash)
                PasswordHash = "100000.EBESExQVFhcYGRobHB0eHw==.u3Fyc5ZjygvMHMkQlV122zvEFBsQzs5gNbr5qrTujjk=",
                IsActive = true,
                CreatedAtUtc = SeedTime,
                UpdatedAtUtc = null,
                CreatedBy = null,
                UpdatedBy = null
            });
    }
}
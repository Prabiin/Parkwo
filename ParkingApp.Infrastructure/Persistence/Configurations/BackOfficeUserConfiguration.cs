using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeUserConfiguration : IEntityTypeConfiguration<BackOfficeUser>
{
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

        builder.HasData(
            new BackOfficeUser
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UserName = "SuperAdmin",
                FullName = "Super Admin",
                Email = "superadmin@gmail.com",
                // PBKDF2 hash of "P@ssw0rd" (iterations.salt.hash)
                PasswordHash = "100000.EBESExQVFhcYGRobHB0eHw==.u3Fyc5ZjygvMHMkQlV122zvEFBsQzs5gNbr5qrTujjk=",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UnixEpoch
            });
    }
}
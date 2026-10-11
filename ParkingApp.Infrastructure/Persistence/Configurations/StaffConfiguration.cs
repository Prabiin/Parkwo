using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staffs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.MustChangePassword)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DateOfBirth);
        builder.Property(x => x.Gender);

        builder.Property(x => x.DeviceName)
            .HasMaxLength(120);

        builder.Property(x => x.DeviceBrand)
            .HasMaxLength(120);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.LastLoginAtUtc);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        // The login key: one Parkwo account per email.
        builder.HasIndex(x => x.Email)
            .IsUnique();

        // The BackOffice staff list filters by facility and status, and the
        // gate authorises by (staff, facility) on every scan.
        builder.HasIndex(x => new { x.FacilityId, x.IsDeleted, x.IsActive })
            .HasDatabaseName("IX_Staffs_Facility_Status");

        builder.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

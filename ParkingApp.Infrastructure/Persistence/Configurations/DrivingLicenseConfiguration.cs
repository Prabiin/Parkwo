using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class DrivingLicenseConfiguration : IEntityTypeConfiguration<DrivingLicense>
{
    public void Configure(EntityTypeBuilder<DrivingLicense> builder)
    {
        builder.ToTable("DrivingLicenses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.LicenseNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Categories)
            .IsRequired()
            .HasColumnType("integer[]");

        builder.Property(x => x.FrontImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BackImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        builder.Property(x => x.ApprovalStatus)
            .IsRequired()
            .HasDefaultValue(ApprovalStatusEnum.Pending);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.User)
            .WithOne(x => x.DrivingLicense)
            .HasForeignKey<DrivingLicense>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.LicenseNumber)
            .IsUnique();
    }
}

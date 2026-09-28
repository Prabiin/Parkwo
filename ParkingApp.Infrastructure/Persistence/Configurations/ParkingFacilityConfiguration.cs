using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class ParkingFacilityConfiguration : IEntityTypeConfiguration<ParkingFacility>
{
    public void Configure(EntityTypeBuilder<ParkingFacility> builder)
    {
        builder.ToTable("ParkingFacilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Latitude);

        builder.Property(x => x.Longitude);

        builder.Property(x => x.Location)
            .HasColumnType("geography (point)");

        builder.Property(x => x.HasMarkedParkingLot)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ApprovalStatus)
            .IsRequired()
            .HasDefaultValue(ApprovalStatusEnum.Pending);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.AverageRating);

        builder.Property(x => x.RatingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.Provider)
            .WithMany(x => x.Facilities)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProviderId);
        builder.HasIndex(x => new { x.ProviderId, x.Name })
            .IsUnique();
        builder.HasIndex(x => x.Location)
            .HasMethod("GIST");
    }
}
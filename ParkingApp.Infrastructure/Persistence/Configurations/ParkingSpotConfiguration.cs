using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class ParkingSpotConfiguration : IEntityTypeConfiguration<ParkingSpot>
{
    public void Configure(EntityTypeBuilder<ParkingSpot> builder)
    {
        builder.ToTable("ParkingSpots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FacilityId)
            .IsRequired();

        builder.Property(x => x.SpotNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.VehicleType)
            .IsRequired();

        builder.Property(x => x.PricePerHourNpr)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.Facility)
            .WithMany(x => x.Spots)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FacilityId);
        builder.HasIndex(x => new { x.FacilityId, x.SpotNumber })
            .IsUnique();
    }
}
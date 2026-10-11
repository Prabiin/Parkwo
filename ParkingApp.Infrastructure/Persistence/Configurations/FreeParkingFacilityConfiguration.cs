using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class FreeParkingFacilityConfiguration : IEntityTypeConfiguration<FreeParkingFacility>
{
    public void Configure(EntityTypeBuilder<FreeParkingFacility> builder)
    {
        builder.ToTable("FreeParkingFacilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Latitude)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasColumnType("geography (point)");

        builder.Property(x => x.TwoWheelerOccupancy);

        builder.Property(x => x.FourWheelerOccupancy);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        // Nearby search filters on both flags, and the map reads only active,
        // non-deleted rows.
        builder.HasIndex(x => new { x.IsActive, x.IsDeleted });

        builder.HasIndex(x => x.Location)
            .HasMethod("GIST");
    }
}

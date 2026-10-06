using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PricePerHourNpr)
            .IsRequired()
            .HasColumnType("numeric(10,2)");

        builder.Property(x => x.TotalAmountNpr)
            .IsRequired()
            .HasColumnType("numeric(12,2)");

        builder.Property(x => x.PlatformFeeNpr)
            .IsRequired()
            .HasColumnType("numeric(12,2)");

        builder.Property(x => x.ProviderAmountNpr)
            .IsRequired()
            .HasColumnType("numeric(12,2)");

        builder.Property(x => x.StartsAtUtc)
            .IsRequired();

        builder.Property(x => x.EndsAtUtc)
            .IsRequired();

        builder.Property(x => x.HoldExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(256);

        builder.Property(x => x.PassNonce)
            .IsRequired();

        builder.Property(x => x.EnteredAtUtc);
        builder.Property(x => x.ExitedAtUtc);
        builder.Property(x => x.SpaceReleasedAtUtc);
        builder.Property(x => x.ActualStayMinutes);
        builder.Property(x => x.OverstayMinutes)
            .IsRequired();

        builder.Property(x => x.OverstayBillableHours);
        builder.Property(x => x.OverstayAmountPaisa);

        builder.HasOne(x => x.EnteredByUser)
            .WithMany()
            .HasForeignKey(x => x.EnteredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ExitedByUser)
            .WithMany()
            .HasForeignKey(x => x.ExitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FacilityId, x.VehicleType, x.StartsAtUtc, x.EndsAtUtc })
            .HasDatabaseName("IX_Bookings_Availability");

        builder.HasIndex(x => new { x.UserId, x.Status });

        // "Which vehicles are physically parked right now, unpaid." Drives the
        // gate dashboard and the sweeper that releases spaces nobody scanned out.
        builder.HasIndex(x => new { x.FacilityId, x.Status })
            .HasDatabaseName("IX_Bookings_Facility_Status");

        // Supports "vehicles still on site past their window" for overstay review.
        builder.HasIndex(x => new { x.Status, x.EndsAtUtc })
            .HasDatabaseName("IX_Bookings_Open_Overstay");

        // "Does this rider owe an unsettled overstay?" — asked on every booking
        // creation, so keep it off the heap. Partial because an on-time exit
        // never participates and would otherwise fill the index.
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Bookings_Unsettled_Overstay")
            .HasFilter("\"OverstayAmountPaisa\" IS NOT NULL");
    }
}
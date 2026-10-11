using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AmountPaisa)
            .IsRequired();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.GatewayPaymentId)
            .HasMaxLength(128);

        builder.Property(x => x.GatewayTransactionId)
            .HasMaxLength(128);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(256);

        // Audit of a staff-confirmed walk-in settlement: who confirmed it and
        // when. Null for a normal gateway-settled payment. The ByUserId column is
        // legacy (rider/org confirmers); new confirmations record the staff id.
        builder.Property(x => x.VerifiedManuallyByUserId);
        builder.Property(x => x.VerifiedManuallyByStaffId);
        builder.Property(x => x.VerifiedManuallyAtUtc);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Gateway)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Gateways retry and replay callbacks, so the gateway handle must be
        // globally unique — this is also the lookup key for reconciliation.
        builder.HasIndex(x => x.GatewayPaymentId)
            .IsUnique()
            .HasFilter("\"GatewayPaymentId\" IS NOT NULL");

        builder.HasIndex(x => x.GatewayTransactionId);

        // A booking can be retried across payments, but only one may ever settle.
        // Enforced in the database so a double callback cannot double-confirm.
        builder.HasIndex(x => x.BookingId)
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)PaymentStatusEnum.Completed}");
    }
}
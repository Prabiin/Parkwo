using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class OverstayPaymentConfiguration : IEntityTypeConfiguration<OverstayPayment>
{
    public void Configure(EntityTypeBuilder<OverstayPayment> builder)
    {
        builder.ToTable("OverstayPayments");

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

        // Gateways replay callbacks, so the handle must be globally unique —
        // also the key the callback handler looks the payment up by.
        builder.HasIndex(x => x.GatewayPaymentId)
            .IsUnique()
            .HasFilter("\"GatewayPaymentId\" IS NOT NULL");

        builder.HasIndex(x => x.GatewayTransactionId);

        // One booking's overstay may be retried across attempts, but only one
        // may ever settle. Enforced in the database so a double callback cannot
        // double-charge — the same protection the prepaid Payments table has,
        // scoped to this table so the two never collide.
        builder.HasIndex(x => x.BookingId)
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)PaymentStatusEnum.Completed}");
    }
}

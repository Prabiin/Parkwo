using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

/// <summary>
/// One attempt to settle a booking's overstay charge — the money owed for
/// minutes held past <see cref="Booking.EndsAtUtc"/> beyond the grace window.
///
/// Deliberately a separate table from <see cref="Payment"/> rather than a row
/// distinguished by a purpose flag. The prepaid charge and the overstay charge
/// have different lifecycles: a prepaid payment lives inside a ten-minute hold
/// on a booking that is still <see cref="BookingStatusEnum.PendingPayment"/>,
/// an overstay payment starts after the booking is already terminal at
/// <see cref="BookingStatusEnum.Completed"/> and has no hold at all. Sharing a
/// table would drag every guard in the prepaid flow into the overstay flow and
/// vice versa. The only thing the two share is the booking they belong to, so
/// history is a join across the two tables.
/// </summary>
public class OverstayPayment : AuditableEntity
{
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }

    public PaymentGatewayEnum Gateway { get; private set; }
    public PaymentStatusEnum Status { get; private set; }

    /// <summary>Snapshot of <see cref="Booking.OverstayAmountPaisa"/>. Never client-supplied.</summary>
    public long AmountPaisa { get; private set; }
    public string Currency { get; private set; } = "NPR";

    /// <summary>Gateway-side handle (Khalti `pidx`). Unique — the lookup key.</summary>
    public string? GatewayPaymentId { get; private set; }

    /// <summary>Gateway's own transaction reference, populated on success.</summary>
    public string? GatewayTransactionId { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public Booking? Booking { get; private set; }

    private OverstayPayment() { }

    public static OverstayPayment Create(
        Guid bookingId,
        Guid userId,
        PaymentGatewayEnum gateway,
        long amountPaisa)
    {
        return new OverstayPayment
        {
            BookingId = bookingId,
            UserId = userId,
            Gateway = gateway,
            Status = PaymentStatusEnum.Initiated,
            AmountPaisa = amountPaisa,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Persists the gateway handle returned by initiate. Safe to call once.</summary>
    public void AttachGatewayPaymentId(string gatewayPaymentId)
    {
        GatewayPaymentId = gatewayPaymentId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string? gatewayTransactionId = null)
    {
        Status = PaymentStatusEnum.Completed;
        PaidAtUtc = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(gatewayTransactionId))
            GatewayTransactionId = gatewayTransactionId;
        FailureReason = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatusEnum.Failed;
        FailureReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = PaymentStatusEnum.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        Status = PaymentStatusEnum.Expired;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = PaymentStatusEnum.Refunded;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsSettled => Status == PaymentStatusEnum.Completed
                             || Status == PaymentStatusEnum.Refunded;
}

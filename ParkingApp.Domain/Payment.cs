using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

/// <summary>
/// One attempt to pay for one booking through one gateway. Amounts are stored
/// in PAISA as integers because that is the unit gateways speak in — a
/// decimal-rupee column invites 100x drift bugs at the boundary.
/// </summary>
public class Payment : AuditableEntity
{
    public Guid BookingId { get; private set; }

    /// <summary>
    /// The account the charge is attributed to. Null for a walk-in payment: the
    /// rider at the gate has no Parkwo account, so the charge is tied to the
    /// booking alone rather than to a fabricated user.
    /// </summary>
    public Guid? UserId { get; private set; }

    public PaymentGatewayEnum Gateway { get; private set; }
    public PaymentStatusEnum Status { get; private set; }

    public long AmountPaisa { get; private set; }
    public string Currency { get; private set; } = "NPR";

    /// <summary>Gateway-side payment index (Khalti `pidx`). Unique — the webhook/lookup key.</summary>
    public string? GatewayPaymentId { get; private set; }

    /// <summary>Gateway's own transaction reference, populated on success.</summary>
    public string? GatewayTransactionId { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Legacy rider-account confirmer. No longer written: manual confirmation is
    /// a staff action (<see cref="VerifiedManuallyByStaffId"/>). Kept for history.
    /// </summary>
    public Guid? VerifiedManuallyByUserId { get; private set; }

    /// <summary>
    /// Set when gate staff confirmed an on-screen walk-in payment before the
    /// gateway callback arrived. Recorded with who and when so a staff-confirmed
    /// settlement is auditable and never indistinguishable from a machine one.
    /// </summary>
    public Guid? VerifiedManuallyByStaffId { get; private set; }

    public DateTimeOffset? VerifiedManuallyAtUtc { get; private set; }

    // Navigation property
    public Booking? Booking { get; private set; }

    private Payment() { }

    public static Payment Create(
        Guid bookingId,
        Guid? userId,
        PaymentGatewayEnum gateway,
        long amountPaisa)
    {
        return new Payment
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

    /// <summary>
    /// Marks this attempt settled on a staff member's on-screen confirmation,
    /// rather than a gateway lookup. The staff id and time are recorded so the
    /// settlement stays attributable and reconcilable if the gateway later
    /// disagrees. Deliberately bypasses <see cref="GatewayTransactionId"/>,
    /// which only a real gateway response may populate.
    /// </summary>
    public void MarkManuallyVerified(Guid staffId)
    {
        Status = PaymentStatusEnum.Completed;
        PaidAtUtc = DateTimeOffset.UtcNow;
        VerifiedManuallyByStaffId = staffId;
        VerifiedManuallyAtUtc = DateTimeOffset.UtcNow;
        FailureReason = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsSettled => Status == PaymentStatusEnum.Completed
                             || Status == PaymentStatusEnum.Refunded;
}
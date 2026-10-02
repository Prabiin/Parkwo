using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

/// <summary>
/// A prepaid reservation of one space of a given vehicle type at a facility,
/// for a bounded time window. The lot declares a capacity per type
/// (ParkingFacility.TwoWheelerOccupancy / FourWheelerOccupancy) but no discrete
/// spots, so a booking reserves "a space of that type, any spot" — availability
/// is the capacity minus the count of window-overlapping live bookings.
/// </summary>
public class Booking : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid FacilityId { get; private set; }
    public Guid VehicleId { get; private set; }
    public VehicleTypeEnum VehicleType { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }

    /// <summary>Hourly rate snapshotted at booking time so later price edits never rewrite history.</summary>
    public decimal PricePerHourNpr { get; private set; }

    public int BillableHours { get; private set; }

    public decimal TotalAmountNpr { get; private set; }

    /// <summary>Platform cut. Kept on the booking so settlement never has to re-derive it.</summary>
    public decimal PlatformFeeNpr { get; private set; }

    public decimal ProviderAmountNpr { get; private set; }

    public BookingStatusEnum Status { get; private set; }

    /// <summary>
    /// When an unpaid hold stops consuming capacity. Khalti's payment link
    /// expires in 60 minutes, so this stays below that window.
    /// </summary>
    public DateTimeOffset HoldExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Random per-booking secret mixed into the gate pass signature. Rotating it
    /// invalidates every QR ever issued for this booking, so a screenshot that
    /// leaked can be cut off without touching the booking's identity.
    /// </summary>
    public Guid PassNonce { get; private set; }

    /// <summary>When the rider physically parked (entry scan).</summary>
    public DateTimeOffset? EnteredAtUtc { get; private set; }

    /// <summary>When the rider left (exit scan). Set together with <see cref="ExitedByUserId"/>.</summary>
    public DateTimeOffset? ExitedAtUtc { get; private set; }

    /// <summary>Gate staff who admitted the vehicle. Keeps entry disputes attributable.</summary>
    public Guid? EnteredByUserId { get; private set; }

    /// <summary>Gate staff who released the vehicle on the way out.</summary>
    public Guid? ExitedByUserId { get; private set; }

    // Navigation properties (gate operators, kept nullable — staff can be removed)
    public User? EnteredByUser { get; private set; }
    public User? ExitedByUser { get; private set; }

    /// <summary>
    /// Measured stay in whole minutes (entry to exit). Null until the exit scan.
    /// This is the real occupancy, independent of the window the rider prepaid.
    /// </summary>
    public int? ActualStayMinutes { get; private set; }

    /// <summary>
    /// Minutes past <see cref="EndsAtUtc"/> at the moment of exit. Zero for an
    /// early or on-time departure; positive means the space was held longer than
    /// it was paid for. Recorded for billing, never charged automatically.
    /// </summary>
    public int OverstayMinutes { get; private set; }

    /// <summary>
    /// When the space actually became free. Normally the exit scan, but the
    /// sweeper can also stamp it for a vehicle that never got scanned out.
    /// Availability reads this instead of <see cref="EndsAtUtc"/> so leaving
    /// early hands the space back immediately rather than at the window's end.
    /// </summary>
    public DateTimeOffset? SpaceReleasedAtUtc { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public ParkingFacility? Facility { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    private Booking() { }

    public static Booking Create(
        Guid userId,
        Guid facilityId,
        Guid vehicleId,
        VehicleTypeEnum vehicleType,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        decimal pricePerHourNpr,
        int billableHours,
        decimal platformFeeNpr,
        int holdMinutes)
    {
        var totalAmount = pricePerHourNpr * billableHours;

        return new Booking
        {
            UserId = userId,
            FacilityId = facilityId,
            VehicleId = vehicleId,
            VehicleType = vehicleType,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            PricePerHourNpr = pricePerHourNpr,
            BillableHours = billableHours,
            TotalAmountNpr = totalAmount,
            PlatformFeeNpr = platformFeeNpr,
            ProviderAmountNpr = totalAmount - platformFeeNpr,
            Status = BookingStatusEnum.PendingPayment,
            HoldExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(holdMinutes),
            PassNonce = Guid.CreateVersion7(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Hold lapsed without payment — releases the space.</summary>
    public void Expire()
    {
        Status = BookingStatusEnum.Expired;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Confirm()
    {
        Status = BookingStatusEnum.Confirmed;
        ConfirmedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gate entry scan. Records when the vehicle actually parked and who let it
    /// in, which is the start of the measured stay. Deliberately takes the
    /// timestamp and the operator id from the caller so the recorded time is
    /// the server's, never the scanner device's (untrusted clocks).
    /// </summary>
    public void Activate(DateTimeOffset enteredAtUtc, Guid scannedByUserId)
    {
        Status = BookingStatusEnum.Active;
        EnteredAtUtc = enteredAtUtc;
        EnteredByUserId = scannedByUserId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gate exit scan. Ends the measured stay and hands the space back. Called
    /// only after an entry scan, so <see cref="EnteredAtUtc"/> is always set and
    /// the stay length is always measurable.
    /// </summary>
    public void Complete(DateTimeOffset exitedAtUtc, Guid scannedByUserId)
    {
        Status = BookingStatusEnum.Completed;
        ExitedAtUtc = exitedAtUtc;
        ExitedByUserId = scannedByUserId;
        SpaceReleasedAtUtc = exitedAtUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Measured, not declared: the stay is whatever the gate actually saw,
        // and the overstay is the part of it the rider did not pay for.
        if (EnteredAtUtc is { } enteredAt)
        {
            ActualStayMinutes = (int)Math.Max(
                0, Math.Floor((exitedAtUtc - enteredAt).TotalMinutes));

            OverstayMinutes = (int)Math.Max(
                0, Math.Floor((exitedAtUtc - EndsAtUtc).TotalMinutes));
        }
    }

    /// <summary>
    /// Releases a space that was never scanned out — the rider drove off, or the
    /// window elapsed while the vehicle was still parked. Keeps the exit
    /// timestamp null so it is never mistaken for a real gate exit, and
    /// attributes the stay to the window the rider paid for.
    /// </summary>
    public void ReleaseSpaceWithoutExit(DateTimeOffset releasedAtUtc)
    {
        SpaceReleasedAtUtc = releasedAtUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Rotates the pass secret, invalidating every QR already handed out for
    /// this booking. Used when a pass leaks (screenshot, shared photo) and the
    /// rider needs a fresh one without cancelling and rebooking.
    /// </summary>
    public void RotatePassNonce()
    {
        PassNonce = Guid.CreateVersion7();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        Status = BookingStatusEnum.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = BookingStatusEnum.Refunded;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
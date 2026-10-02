using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// What the gate is allowed to do with a pass, decided before any row is
/// touched. Kept separate from the handlers so the rules are unit-testable and
/// so a scan can explain itself ("too early") without a second round trip.
/// </summary>
public static class GateRules
{
    /// <summary>May this booking's pass be used to park a vehicle right now?</summary>
    public static ScanOutcomeEnum CheckEntry(
        BookingStatusEnum status,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        DateTimeOffset nowUtc,
        int earlyEntryGraceMinutes,
        int lateEntryGraceMinutes)
    {
        // Only a settled booking gets a pass, so anything unpaid is a hard stop
        // even if a pass was somehow produced for it.
        if (status == BookingStatusEnum.PendingPayment)
            return ScanOutcomeEnum.NotPaid;

        if (status is BookingStatusEnum.Cancelled
            or BookingStatusEnum.Expired
            or BookingStatusEnum.Refunded
            or BookingStatusEnum.Completed)
            return ScanOutcomeEnum.InvalidState;

        if (status == BookingStatusEnum.Active)
            return ScanOutcomeEnum.AlreadyParked;

        if (nowUtc < startsAtUtc.AddMinutes(-Math.Max(0, earlyEntryGraceMinutes)))
            return ScanOutcomeEnum.TooEarly;

        if (nowUtc > endsAtUtc.AddMinutes(Math.Max(0, lateEntryGraceMinutes)))
            return ScanOutcomeEnum.TooLate;

        return ScanOutcomeEnum.Accepted;
    }

    /// <summary>May this booking's pass be used to release a space right now?</summary>
    public static ScanOutcomeEnum CheckExit(BookingStatusEnum status)
        => status switch
        {
            BookingStatusEnum.Active => ScanOutcomeEnum.Accepted,
            BookingStatusEnum.Completed => ScanOutcomeEnum.AlreadyClosed,
            _ => ScanOutcomeEnum.NotCheckedIn
        };

    /// <summary>Whether an outcome is a rejection staff must act on rather than a note.</summary>
    public static bool IsRejection(ScanOutcomeEnum outcome)
        => outcome is not ScanOutcomeEnum.Accepted
           and not ScanOutcomeEnum.AlreadyParked
           and not ScanOutcomeEnum.AlreadyClosed
           and not ScanOutcomeEnum.Overstay;
}

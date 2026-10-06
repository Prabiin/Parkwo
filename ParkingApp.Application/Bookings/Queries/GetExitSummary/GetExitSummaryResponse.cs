using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetExitSummary;

/// <summary>
/// The exit page the rider reaches from the button next to the QR. Drives the
/// whole overstay flow without any state of its own: <see cref="ExitRecorded"/>
/// toggles the button, <see cref="OverstayDetected"/> routes to the overstay
/// page, and <see cref="OverstayPaymentStatus"/> decides whether that page
/// offers to pay or shows a receipt.
/// </summary>
public record GetExitSummaryResponse(
    Guid BookingId,
    BookingStatusEnum Status,
    bool ExitRecorded,
    string? BookingStartedAtLocal,
    string? BookingEndsAtLocal,
    string? EnteredAtLocal,
    string? ExitedAtLocal,
    int? ActualStayMinutes,
    int EarlyEntryGraceMinutes,
    int OverstayGraceMinutes,
    int OverstayMinutes,
    bool OverstayDetected,
    int? OverstayBillableHours,
    decimal? OverstayAmountNpr,
    string? Message,
    PaymentStatusEnum? OverstayPaymentStatus);

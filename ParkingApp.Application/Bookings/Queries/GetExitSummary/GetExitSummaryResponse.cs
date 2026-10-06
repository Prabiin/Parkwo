using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetExitSummary;

/// <summary>
/// What the rider's exit page shows. Times come back already in local
/// (Kathmandu) format and money in rupees, so the app can render them
/// directly.
/// </summary>
public record GetExitSummaryResponse(
    Guid BookingId,
    BookingStatusEnum Status,
    bool ExitRecorded,
    string? StartsAtLocal,
    string? EndsAtLocal,
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
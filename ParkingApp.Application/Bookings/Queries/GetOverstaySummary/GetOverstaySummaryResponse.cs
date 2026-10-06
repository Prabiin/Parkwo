using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetOverstaySummary;

/// <summary>
/// The confirmation screen shown before an overstay payment is started, so the
/// rider sees the rate, the hours being charged and the exact amount in tuning
/// rupees before Khalti takes anything. Amount comes from the charge frozen at
/// the exit scan — the same figure the payment will actually collect.
/// </summary>
public record GetOverstaySummaryResponse(
    Guid BookingId,
    string FacilityName,
    string StartsAtLocal,
    string EndsAtLocal,
    string? EnteredAtLocal,
    string? ExitedAtLocal,
    int OverstayMinutes,
    int GraceMinutes,
    int BillableHours,
    decimal PricePerHourNpr,
    decimal OverstayAmountNpr,
    string Currency,
    PaymentStatusEnum? OverstayPaymentStatus);
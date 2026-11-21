using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.Shared;

/// <summary>
/// One booking as the rider's Bookings tab, Home card and active-stay screen
/// show it. <see cref="Status"/> is the effective status: an unpaid hold whose
/// 5-minute window has lapsed reads as Expired even before anything has
/// written that to the row.
/// </summary>
public sealed record RiderBookingItemResponse(
    Guid BookingId,
    BookingStatusEnum Status,
    string StatusDescription,
    Guid FacilityId,
    string FacilityName,
    string FacilityAddress,
    Guid? VehicleId,
    string? VehicleName,
    string VehicleNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string StartsAtLocal,
    string EndsAtLocal,
    DateTimeOffset? HoldExpiresAtUtc,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset? ExitedAtUtc,
    int? ActualStayMinutes,
    decimal BookingAmountNpr,
    decimal? OverstayAmountNpr,
    bool OverstayDue,
    decimal TotalPaidNpr);

/// <summary>
/// The columns a <see cref="RiderBookingItemResponse"/> is built from. A
/// class (not an anonymous type) so the same SQL projection is shared by the
/// list and the active-booking queries.
/// </summary>
public sealed class RiderBookingRow
{
    public Guid Id { get; init; }
    public BookingStatusEnum Status { get; init; }
    public Guid FacilityId { get; init; }
    public string FacilityName { get; init; } = default!;
    public string FacilityAddress { get; init; } = default!;
    public Guid? VehicleId { get; init; }
    public string? VehicleName { get; init; }
    public string? VehicleNumber { get; init; }
    public VehicleTypeEnum VehicleType { get; init; }
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public DateTimeOffset HoldExpiresAtUtc { get; init; }
    public DateTimeOffset? EnteredAtUtc { get; init; }
    public DateTimeOffset? ExitedAtUtc { get; init; }
    public int? ActualStayMinutes { get; init; }
    public decimal TotalAmountNpr { get; init; }
    public long? OverstayAmountPaisa { get; init; }
    public bool OverstaySettled { get; init; }
}

public static class RiderBookings
{
    /// <summary>Statuses whose prepaid charge was actually collected.</summary>
    private static readonly BookingStatusEnum[] PaidStatuses =
    [
        BookingStatusEnum.Confirmed,
        BookingStatusEnum.Active,
        BookingStatusEnum.Completed
    ];

    /// <summary>
    /// Bookings still ahead of the rider: paid and not yet over, parked right
    /// now, or an unpaid hold that has not lapsed.
    /// </summary>
    public static IQueryable<Booking> Upcoming(IQueryable<Booking> source, DateTimeOffset nowUtc)
        => source.Where(b => b.Status == BookingStatusEnum.Confirmed
                             || b.Status == BookingStatusEnum.Active
                             || (b.Status == BookingStatusEnum.PendingPayment && b.HoldExpiresAtUtc > nowUtc));

    /// <summary>Everything that is no longer upcoming: finished, cancelled or lapsed.</summary>
    public static IQueryable<Booking> History(IQueryable<Booking> source, DateTimeOffset nowUtc)
        => source.Where(b => b.Status == BookingStatusEnum.Completed
                             || b.Status == BookingStatusEnum.Cancelled
                             || b.Status == BookingStatusEnum.Expired
                             || b.Status == BookingStatusEnum.Refunded
                             || (b.Status == BookingStatusEnum.PendingPayment && b.HoldExpiresAtUtc <= nowUtc));

    /// <summary>
    /// The shared SQL projection. "Settled" uses the same test as the
    /// create-booking gate: a completed or refunded overstay payment exists.
    /// </summary>
    public static IQueryable<RiderBookingRow> Project(IApplicationDbContext context, IQueryable<Booking> source)
        => source.Select(b => new RiderBookingRow
        {
            Id = b.Id,
            Status = b.Status,
            FacilityId = b.FacilityId,
            FacilityName = b.Facility!.Name,
            FacilityAddress = b.Facility!.Address,
            VehicleId = b.VehicleId,
            VehicleName = b.Vehicle != null ? b.Vehicle.Name : null,
            VehicleNumber = b.Vehicle != null ? b.Vehicle.VehicleNumber : b.WalkInPlateNumber,
            VehicleType = b.VehicleType,
            StartsAtUtc = b.StartsAtUtc,
            EndsAtUtc = b.EndsAtUtc,
            HoldExpiresAtUtc = b.HoldExpiresAtUtc,
            EnteredAtUtc = b.EnteredAtUtc,
            ExitedAtUtc = b.ExitedAtUtc,
            ActualStayMinutes = b.ActualStayMinutes,
            TotalAmountNpr = b.TotalAmountNpr,
            OverstayAmountPaisa = b.OverstayAmountPaisa,
            OverstaySettled = context.OverstayPayments.Any(p => p.BookingId == b.Id
                && (p.Status == PaymentStatusEnum.Completed || p.Status == PaymentStatusEnum.Refunded))
        });

    public static RiderBookingItemResponse ToResponse(RiderBookingRow r, DateTimeOffset nowUtc)
    {
        var status = r.Status == BookingStatusEnum.PendingPayment && r.HoldExpiresAtUtc <= nowUtc
            ? BookingStatusEnum.Expired
            : r.Status;

        var overstayNpr = r.OverstayAmountPaisa is { } paisa ? BookingPricing.ToNpr(paisa) : (decimal?)null;
        var overstayDue = overstayNpr is not null && !r.OverstaySettled;

        var totalPaid = (PaidStatuses.Contains(r.Status) ? r.TotalAmountNpr : 0m)
                        + (overstayNpr is not null && r.OverstaySettled ? overstayNpr.Value : 0m);

        return new RiderBookingItemResponse(
            r.Id,
            status,
            status.ToDescription(),
            r.FacilityId,
            r.FacilityName,
            r.FacilityAddress,
            r.VehicleId,
            r.VehicleName,
            r.VehicleNumber ?? string.Empty,
            r.VehicleType,
            r.VehicleType.ToDescription(),
            r.StartsAtUtc,
            r.EndsAtUtc,
            LocalTime.Format(r.StartsAtUtc),
            LocalTime.Format(r.EndsAtUtc),
            status == BookingStatusEnum.PendingPayment ? r.HoldExpiresAtUtc : null,
            r.EnteredAtUtc,
            r.ExitedAtUtc,
            r.ActualStayMinutes,
            r.TotalAmountNpr,
            overstayNpr,
            overstayDue,
            totalPaid);
    }
}

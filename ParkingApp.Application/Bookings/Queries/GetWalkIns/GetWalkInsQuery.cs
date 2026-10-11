using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetWalkIns;

/// <summary>
/// The gate's walk-in board: everything staff opened at one facility, newest
/// first, optionally filtered to a state. The unpaid-hold filter is the
/// "chase this rider before the hold lapses" list; the Active filter is the
/// "who is still parked" list staff use when a QR will not scan.
/// </summary>
public sealed record GetWalkInsQuery(Guid FacilityId, BookingStatusEnum? Status)
    : IRequestResult<GetWalkInsQuery, GetWalkInsResponse>;

public sealed record GetWalkInsResponse(IReadOnlyList<WalkInBookingItem> Items);

/// <summary>
/// One walk-in as staff see it. The full snapshot the gate captured at creation
/// travels with it — name, phone, plate, vehicle type, purchased hours and the
/// amount — so the board needs no second lookup and, at an exit, staff can match
/// the physical vehicle's plate and check it out without the rider's QR.
/// </summary>
public sealed record WalkInBookingItem(
    Guid BookingId,
    BookingStatusEnum Status,
    string StatusDescription,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    string RiderName,
    string PhoneNumber,
    string PlateNumber,
    int BillableHours,
    decimal PricePerHourNpr,
    decimal TotalAmountNpr,
    long AmountPaisa,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset? ExitedAtUtc,
    DateTimeOffset HoldExpiresAtUtc,
    int? ActualStayMinutes,
    int OverstayMinutes,
    long? OverstayAmountPaisa,
    DateTimeOffset CreatedAtUtc);

public sealed class GetWalkInsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetWalkInsQuery, GetWalkInsResponse>
{
    public async Task<Result<GetWalkInsResponse>> Handle(
        GetWalkInsQuery request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<GetWalkInsResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<GetWalkInsResponse>.Failure(
                "You are not authorised to view walk-ins at this facility.", 403);

        var query = context.Bookings
            .AsNoTracking()
            .Where(b => b.FacilityId == request.FacilityId && b.IsWalkInBooking);

        if (request.Status is { } status)
            query = query.Where(b => b.Status == status);

        var rows = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.VehicleType,
                b.WalkInRiderName,
                b.WalkInPhoneNumber,
                b.WalkInPlateNumber,
                b.BillableHours,
                b.PricePerHourNpr,
                b.TotalAmountNpr,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.EnteredAtUtc,
                b.ExitedAtUtc,
                b.HoldExpiresAtUtc,
                b.ActualStayMinutes,
                b.OverstayMinutes,
                b.OverstayAmountPaisa,
                b.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(b => new WalkInBookingItem(
                b.Id,
                b.Status,
                b.Status.ToDescription(),
                b.VehicleType,
                b.VehicleType.ToDescription(),
                b.WalkInRiderName ?? string.Empty,
                b.WalkInPhoneNumber ?? string.Empty,
                b.WalkInPlateNumber ?? string.Empty,
                b.BillableHours,
                b.PricePerHourNpr,
                b.TotalAmountNpr,
                BookingPricing.ToPaisa(b.TotalAmountNpr),
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.EnteredAtUtc,
                b.ExitedAtUtc,
                b.HoldExpiresAtUtc,
                b.ActualStayMinutes,
                b.OverstayMinutes,
                b.OverstayAmountPaisa,
                b.CreatedAtUtc))
            .ToList();

        return Result<GetWalkInsResponse>.Success(new GetWalkInsResponse(items));
    }
}

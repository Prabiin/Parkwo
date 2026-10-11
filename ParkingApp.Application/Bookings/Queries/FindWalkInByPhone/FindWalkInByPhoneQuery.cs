using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.GetWalkIns;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.FindWalkInByPhone;

/// <summary>
/// The exit fallback when a rider's photographed QR will not scan: staff type
/// the phone number the walk-in was opened under and get back the vehicles
/// opened under it <b>today at that facility</b>. The day scope matches the
/// shift staff are working, and the result carries the whole creation snapshot —
/// name, phone, plate, vehicle type, purchased hours and amount — so the staff
/// member can match the physical vehicle (e.g. its plate) and then close the
/// stay through the CompleteWalkInExit command instead of the scan. Returns
/// every match rather than one, so a person who parked two vehicles gets a
/// picker instead of a silent wrong choice. A lookup only — the exit itself
/// prices any overstay exactly as the scan does.
/// </summary>
public sealed record FindWalkInByPhoneQuery(Guid FacilityId, string PhoneNumber)
    : IRequestResult<FindWalkInByPhoneQuery, FindWalkInByPhoneResponse>;

public sealed record FindWalkInByPhoneResponse(IReadOnlyList<WalkInBookingItem> Matches);

public sealed class FindWalkInByPhoneQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<FindWalkInByPhoneQuery, FindWalkInByPhoneResponse>
{
    public async Task<Result<FindWalkInByPhoneResponse>> Handle(
        FindWalkInByPhoneQuery request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<FindWalkInByPhoneResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<FindWalkInByPhoneResponse>.Failure(
                "You are not authorised to look up walk-ins at this facility.", 403);

        // Both sides are stored normalised, so the staff member may type the
        // number with or without the country code or separators.
        var phone = PhoneNumbers.Normalize(request.PhoneNumber);

        // "That day" is a Kathmandu day, not a UTC one, so a lookup just after
        // local midnight still matches the shift the staff are on.
        var dayStart = LocalTime.StartOfTodayUtc();
        var dayEnd = LocalTime.EndOfTodayUtc();

        var rows = await context.Bookings
            .AsNoTracking()
            .Where(b => b.FacilityId == request.FacilityId
                        && b.IsWalkInBooking
                        && b.Status == BookingStatusEnum.Active
                        && b.WalkInPhoneNumber == phone
                        && b.CreatedAtUtc >= dayStart
                        && b.CreatedAtUtc < dayEnd)
            .OrderBy(b => b.EnteredAtUtc)
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

        var matches = rows
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

        return Result<FindWalkInByPhoneResponse>.Success(new FindWalkInByPhoneResponse(matches));
    }
}

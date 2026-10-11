using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.GetBookingPass;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetWalkInPass;

/// <summary>
/// A staff device issues the walk-in rider the very same gate pass an app rider
/// would get — same <see cref="ParkingPassService"/>, same token, same gate
/// verification. Ownership is the only difference: a walk-in has no rider
/// account to be checked against, so the caller is authorised as facility staff
/// instead. The rider photographs the QR and shows it at entry and exit; the
/// photograph is a copy of a legitimate pass, not a weaker one.
/// </summary>
public sealed record GetWalkInPassQuery(Guid BookingId)
    : IRequestResult<GetWalkInPassQuery, GetBookingPassResponse>;

public sealed class GetWalkInPassQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ParkingPassService passes)
    : IRequestResultHandler<GetWalkInPassQuery, GetBookingPassResponse>
{
    public async Task<Result<GetBookingPassResponse>> Handle(
        GetWalkInPassQuery request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<GetBookingPassResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.IsWalkInBooking,
                b.FacilityId,
                FacilityName = b.Facility != null ? b.Facility.Name : "",
                VehicleNumber = b.Vehicle != null ? b.Vehicle.VehicleNumber : b.WalkInPlateNumber ?? "",
                b.VehicleType,
                b.Status,
                b.StartsAtUtc,
                b.EndsAtUtc,
                b.PassNonce,
                b.EnteredAtUtc,
                b.ExitedAtUtc,
                b.ActualStayMinutes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return Result<GetBookingPassResponse>.Failure("Booking not found.", 404);

        if (!booking.IsWalkInBooking)
            return Result<GetBookingPassResponse>.Failure("This is not a walk-in booking.", 403);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, booking.FacilityId, cancellationToken))
            return Result<GetBookingPassResponse>.Failure(
                "You are not authorised to issue passes at this facility.", 403);

        if (!passes.IsConfigured)
            return Result<GetBookingPassResponse>.Failure(ParkingPassService.NotConfiguredMessage, 503);

        if (booking.Status == BookingStatusEnum.PendingPayment)
            return Result<GetBookingPassResponse>.Failure(
                "Confirm the walk-in payment before issuing a gate pass.", 409);

        if (booking.Status is BookingStatusEnum.Cancelled
            or BookingStatusEnum.Expired
            or BookingStatusEnum.Refunded)
            return Result<GetBookingPassResponse>.Failure(
                $"A {booking.Status.ToDescription().ToLowerInvariant()} booking has no gate pass.", 409);

        var validity = passes.ValidityFor(booking.StartsAtUtc, booking.EndsAtUtc);

        return Result<GetBookingPassResponse>.Success(
            new GetBookingPassResponse(
                booking.Id,
                booking.FacilityId,
                booking.FacilityName,
                booking.VehicleNumber,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                validity.From,
                validity.Until,
                booking.Status,
                booking.Status.ToDescription(),
                passes.Issue(booking.Id, booking.FacilityId, booking.PassNonce, validity.From, validity.Until),
                booking.EnteredAtUtc,
                booking.ExitedAtUtc,
                booking.ActualStayMinutes));
    }
}

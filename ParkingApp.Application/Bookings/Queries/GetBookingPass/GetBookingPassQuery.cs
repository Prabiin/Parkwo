using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingPass;

/// <summary>
/// Hands the rider the QR payload for a booking they have already paid for.
/// The pass is derived, not stored: any change to the booking or to
/// <see cref="Booking.PassNonce"/> changes the signature, so a stale screenshot
/// stops verifying the moment the booking moves on.
/// </summary>
public sealed record GetBookingPassQuery(Guid BookingId)
    : IRequestResult<GetBookingPassQuery, GetBookingPassResponse>;

public sealed record GetBookingPassResponse(
    Guid BookingId,
    Guid FacilityId,
    string FacilityName,
    string VehicleNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc,
    BookingStatusEnum Status,
    string StatusDescription,
    string PassToken,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset? ExitedAtUtc,
    int? ActualStayMinutes);

public sealed class GetBookingPassQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ParkingPassService passes)
    : IRequestResultHandler<GetBookingPassQuery, GetBookingPassResponse>
{
    public async Task<Result<GetBookingPassResponse>> Handle(
        GetBookingPassQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetBookingPassResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.FacilityId,
                FacilityName = b.Facility!.Name,
                b.VehicleId,
                VehicleNumber = b.Vehicle!.VehicleNumber,
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

        if (booking.UserId != userId.Value)
            return Result<GetBookingPassResponse>.Failure("You can only view your own booking pass.", 403);

        // Checked before anything is loaded: an unconfigured environment should
        // say so plainly, not report the booking as missing or unpaid.
        if (!passes.IsConfigured)
            return Result<GetBookingPassResponse>.Failure(ParkingPassService.NotConfiguredMessage, 503);

        // A pass exists to admit a car. Issuing one for an unpaid or dead booking
        // would let a rider show up with a valid-looking QR the gate must refuse.
        if (booking.Status == BookingStatusEnum.PendingPayment)
            return Result<GetBookingPassResponse>.Failure("Pay for this booking before requesting a gate pass.", 409);

        if (booking.Status is BookingStatusEnum.Cancelled
            or BookingStatusEnum.Expired
            or BookingStatusEnum.Refunded)
            return Result<GetBookingPassResponse>.Failure($"A {booking.Status.ToDescription().ToLowerInvariant()} booking has no gate pass.", 409);

        // ValidFrom is the window start, not "now": a rider refreshing the QR an
        // hour early still gets a pass that works from the moment they may enter.
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

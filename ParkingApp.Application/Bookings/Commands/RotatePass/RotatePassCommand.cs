using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.RotatePass;

/// <summary>
/// Invalidates every QR already issued for a booking and returns a fresh one.
/// The escape hatch for a leaked pass: screenshot shared in a group chat,
/// phone stolen, photo left in a gallery. Rotating the nonce makes the old
/// codes fail signature verification without cancelling the booking or touching
/// payment, so the rider keeps the space they paid for.
/// </summary>
public sealed record RotatePassCommand(Guid BookingId)
    : IRequestResult<RotatePassCommand, RotatePassResponse>;

public sealed record RotatePassResponse(
    Guid BookingId,
    string PassToken,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc);

public sealed class RotatePassCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ParkingPassService passes)
    : IRequestResultHandler<RotatePassCommand, RotatePassResponse>
{
    public async Task<Result<RotatePassResponse>> Handle(
        RotatePassCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<RotatePassResponse>.Failure("Authentication required.", 401);

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result<RotatePassResponse>.Failure("Booking not found.", 404);

        if (booking.UserId != userId.Value)
            return Result<RotatePassResponse>.Failure("You can only rotate your own booking pass.", 403);

        if (!passes.IsConfigured)
            return Result<RotatePassResponse>.Failure(
                ParkingPassService.NotConfiguredMessage, 503);

        if (booking.Status is BookingStatusEnum.Cancelled
            or BookingStatusEnum.Expired
            or BookingStatusEnum.Refunded)
            return Result<RotatePassResponse>.Failure(
                $"A {booking.Status.ToDescription().ToLowerInvariant()} booking has no gate pass.", 409);

        // Mirrors GetBookingPassQuery so a rotated pass has exactly the same
        // validity window the rider was told about originally.
        var settings = passes.ValidityFor(booking.StartsAtUtc, booking.EndsAtUtc);

        booking.RotatePassNonce();
        await context.SaveChangesAsync(cancellationToken);

        return Result<RotatePassResponse>.Success(
            new RotatePassResponse(
                booking.Id,
                passes.Issue(booking.Id, booking.FacilityId, booking.PassNonce, settings.From, settings.Until),
                settings.From,
                settings.Until));
    }
}

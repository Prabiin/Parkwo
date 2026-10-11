using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.CompleteWalkIn;

/// <summary>
/// Closes a walk-in stay that was resolved by phone rather than by scanning the
/// rider's QR — the fallback when the photographed pass will not read. It runs
/// the exact same exit the QR scan would: the server clock measures the stay,
/// the overstay is priced and snapshotted onto the booking in the same
/// transaction, and the space is released. Only the way the booking was found
/// differs, so the overstay settlement the rider is offered is identical.
/// </summary>
public sealed record CompleteWalkInExitCommand(
    Guid BookingId,
    Guid FacilityId)
    : IRequestResult<CompleteWalkInExitCommand, CompleteWalkInExitResponse>;

public sealed record CompleteWalkInExitResponse(
    Guid BookingId,
    BookingStatusEnum BookingStatus,
    string BookingStatusDescription,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    string RiderName,
    string? PlateNumber,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset ExitedAtUtc,
    int? ActualStayMinutes,
    int OverstayMinutes,
    int OverstayGraceMinutes,
    bool OverstayDetected,
    int? OverstayBillableHours,
    long? OverstayAmountPaisa,
    string Message);

public sealed class CompleteWalkInExitCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IOptions<PassSettings> passSettings)
    : IRequestResultHandler<CompleteWalkInExitCommand, CompleteWalkInExitResponse>
{
    public async Task<Result<CompleteWalkInExitResponse>> Handle(
        CompleteWalkInExitCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<CompleteWalkInExitResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<CompleteWalkInExitResponse>.Failure(
                "You are not authorised to record exits at this facility.", 403);

        // The server's clock is the billing clock; a phone with a skewed clock
        // must not be able to shorten a stay.
        var now = DateTimeOffset.UtcNow;
        var settings = passSettings.Value;

        // SERIALIZABLE for the same reason the scan is: two taps must not both
        // close the stay and both record an overstay.
        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        var booking = await context.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<CompleteWalkInExitResponse>.Failure("Booking not found.", 404);
        }

        if (!booking.IsWalkInBooking || booking.FacilityId != request.FacilityId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<CompleteWalkInExitResponse>.Failure(
                "This is not a walk-in booking at this facility.", 404);
        }

        var outcome = GateRules.CheckExit(booking.Status);
        if (outcome != ScanOutcomeEnum.Accepted)
        {
            await transaction.RollbackAsync(cancellationToken);

            var message = outcome == ScanOutcomeEnum.AlreadyClosed
                ? "This walk-in is already closed out."
                : "No entry was recorded for this walk-in, so there is no stay to close.";

            return Result<CompleteWalkInExitResponse>.Failure(message, 409);
        }

        booking.Complete(now, staffId.Value);

        // Priced only after Complete(), which is what fills in OverstayMinutes,
        // and snapshotted in the same transaction as the exit: the figure staff
        // see and the figure the rider pays are one number, frozen here.
        var charge = OverstayPricing.Calculate(
            booking.PricePerHourNpr,
            booking.OverstayMinutes,
            settings.OverstayGraceMinutes);

        if (charge is { } overstayCharge)
            booking.RecordOverstayCharge(overstayCharge.BillableHours, overstayCharge.AmountPaisa);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var isOverstay = StayCalculator.IsOverstay(
            booking.OverstayMinutes,
            settings.OverstayGraceMinutes);

        return Result<CompleteWalkInExitResponse>.Success(
            new CompleteWalkInExitResponse(
                booking.Id,
                booking.Status,
                booking.Status.ToDescription(),
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.WalkInRiderName ?? string.Empty,
                booking.WalkInPlateNumber,
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.EnteredAtUtc,
                now,
                booking.ActualStayMinutes,
                booking.OverstayMinutes,
                settings.OverstayGraceMinutes,
                isOverstay,
                charge?.BillableHours,
                charge?.AmountPaisa,
                OverstayPricing.DescribeExitMessage(
                    booking.OverstayMinutes,
                    settings.OverstayGraceMinutes,
                    charge)));
    }
}

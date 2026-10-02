using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.ScanExit;

/// <summary>
/// Gate exit scan. Ends the measured stay and releases the space.
/// <para>
/// This is what actually makes capacity available again. Availability counts a
/// booking's occupied window, so without a recorded release time a space stays
/// blocked until the prepaid window ends — a lot full of cars that already left
/// would report zero free spaces for hours.
/// </para>
/// <para>
/// SERIALIZABLE for the same reason as entry: two taps must not both close the
/// stay and both record an overstay.
/// </para>
/// </summary>
public sealed record ScanExitCommand(
    string PassToken,
    Guid FacilityId)
    : IRequestResult<ScanExitCommand, ScanExitResponse>;

public sealed record ScanExitResponse(
    ScanOutcomeEnum Outcome,
    string OutcomeDescription,
    Guid BookingId,
    string VehicleNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset? ReleasedAtUtc,
    int? ActualStayMinutes,
    int OverstayMinutes,
    DateTimeOffset ScannedAtUtc,
    string? Message);

public sealed class ScanExitCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IOptions<PassSettings> passSettings,
    ParkingPassService passes)
    : IRequestResultHandler<ScanExitCommand, ScanExitResponse>
{
    public async Task<Result<ScanExitResponse>> Handle(
        ScanExitCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.UserId;
        if (staffId is null)
            return Result<ScanExitResponse>.Failure("Authentication required.", 401);

        if (!passes.IsConfigured)
            return Result<ScanExitResponse>.Failure(
                ParkingPassService.NotConfiguredMessage, 503);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<ScanExitResponse>.Failure("You are not authorised to scan passes at this facility.", 403);

        var now = DateTimeOffset.UtcNow;
        var settings = passSettings.Value;

        // Deliberately NOT time-checked: a rider whose window has elapsed is
        // exactly the person who must be let out. Verify() would reject them for
        // being overdue, so the signature is checked on its own here.
        var claims = passes.VerifySignatureOnly(request.PassToken);
        if (claims is null)
            return Result<ScanExitResponse>.Failure(
                passes.IsExpired(request.PassToken, now)
                    ? "This pass has expired. Ask the rider to speak to staff."
                    : "This pass is not valid.",
                400);

        if (claims.FacilityId != request.FacilityId)
            return Result<ScanExitResponse>.Failure("This pass belongs to a different facility.", 403);

        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        var booking = await context.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == claims.BookingId, cancellationToken);

        if (booking is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanExitResponse>.Failure("Booking not found.", 404);
        }

        if (booking.PassNonce != claims.PassNonce)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanExitResponse>.Failure(
                "This pass has been replaced. Ask the rider to refresh their QR.", 409);
        }

        if (booking.FacilityId != request.FacilityId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanExitResponse>.Failure("This booking is not for this facility.", 403);
        }

        var outcome = GateRules.CheckExit(booking.Status);

        if (outcome != ScanOutcomeEnum.Accepted)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<ScanExitResponse>.Failure(ExitRefusal(outcome, booking.Status), 409);
        }

        // Complete() measures the stay itself from the entry timestamp it holds,
        // so the recorded numbers and the gate's reported numbers cannot drift.
        booking.Complete(now, staffId.Value);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Recorded on the row, never charged. Charging overstay needs a tariff we
        // do not have, and a wrong automatic charge is far worse than a manual one.
        var isOverstay = StayCalculator.IsOverstay(
            booking.OverstayMinutes,
            settings.OverstayGraceMinutes);
        var finalOutcome = isOverstay ? ScanOutcomeEnum.Overstay : ScanOutcomeEnum.Accepted;

        var exitMessage = isOverstay
            ? $"Exit recorded. Vehicle overstayed by {booking.OverstayMinutes} minutes — refer to staff."
            : "Exit recorded. Space released.";

        return Result<ScanExitResponse>.Success(
            new ScanExitResponse(
                finalOutcome,
                finalOutcome.ToDescription(),
                booking.Id,
                booking.Vehicle?.VehicleNumber ?? string.Empty,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.EnteredAtUtc,
                booking.SpaceReleasedAtUtc,
                booking.ActualStayMinutes,
                booking.OverstayMinutes,
                now,
                exitMessage));
    }

    private static string ExitRefusal(ScanOutcomeEnum outcome, BookingStatusEnum status)
        => outcome switch
        {
            ScanOutcomeEnum.AlreadyClosed => "This booking is already closed out.",
            ScanOutcomeEnum.NotCheckedIn when status == BookingStatusEnum.PendingPayment =>
                "This booking has not been paid.",
            _ => "No entry scan was recorded for this booking, so there is no stay to close."
        };
}

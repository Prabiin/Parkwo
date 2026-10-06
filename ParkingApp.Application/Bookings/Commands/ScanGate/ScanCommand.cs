using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.ScanGate;

/// <summary>
/// Single-gate scan. The same QR is presented at one gate staffed by one phone
/// with one app: the first scan admits the vehicle, the next scan releases it.
/// The direction is not chosen by the caller — the booking's own state decides
/// it (see <see cref="GateRules.DirectionFor"/>), so there is no button a tired
/// attendant can press wrong.
/// <para>
/// SERIALIZABLE for the same reason the two-scan flow was: two taps must not
/// both admit on the way in, nor both close and both record an overstay on the
/// way out.
/// </para>
/// </summary>
public sealed record ScanGateCommand(
    string PassToken,
    Guid FacilityId)
    : IRequestResult<ScanGateCommand, ScanGateResponse>;

// Direction is which phase the scan was: Entry on the way in, Exit on the way
// out. OverstayDetected means money is owed, which is not the same as Outcome
// being Overstay: a free facility can have an overstay worth flagging and
// nothing worth paying. OverstayAmountPaisa is the snapshot the rider is later
// charged, so staff, rider and gateway all see one number.
public sealed record ScanGateResponse(
    ScanTypeEnum Direction,
    ScanOutcomeEnum Outcome,
    string OutcomeDescription,
    Guid BookingId,
    string VehicleNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? EnteredAtUtc,
    DateTimeOffset? ExitedAtUtc,
    DateTimeOffset? ReleasedAtUtc,
    int? ActualStayMinutes,
    int OverstayMinutes,
    int EarlyEntryGraceMinutes,
    int OverstayGraceMinutes,
    bool OverstayDetected,
    int? OverstayBillableHours,
    long? OverstayAmountPaisa,
    DateTimeOffset ScannedAtUtc,
    string? Message);

public sealed class ScanGateCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IOptions<PassSettings> passSettings,
    ParkingPassService passes)
    : IRequestResultHandler<ScanGateCommand, ScanGateResponse>
{
    public async Task<Result<ScanGateResponse>> Handle(
        ScanGateCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.UserId;
        if (staffId is null)
            return Result<ScanGateResponse>.Failure("Authentication required.", 401);

        if (!passes.IsConfigured)
            return Result<ScanGateResponse>.Failure(ParkingPassService.NotConfiguredMessage, 503);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<ScanGateResponse>.Failure("You are not authorised to scan passes at this facility.", 403);

        // The server's clock is the billing clock. A scanner with a skewed clock
        // must not be able to shorten a stay.
        var now = DateTimeOffset.UtcNow;
        var settings = passSettings.Value;

        // Signature and shape only. The time window is deliberately NOT applied
        // here: the gate rules below decide, using the booking's real window, so
        // a too-early rider gets "your window has not opened" instead of a
        // generic rejection. Verify() is used by the pass query instead.
        var claims = passes.VerifySignatureOnly(request.PassToken);
        if (claims is null)
        {
            // Distinguish "expired, rebook" from "forged, call support" — the
            // rider's next step is completely different in each case.
            return passes.IsExpired(request.PassToken, now)
                ? Result<ScanGateResponse>.Failure("This pass has expired. The rider must book again.", 409)
                : Result<ScanGateResponse>.Failure("This pass is not valid.", 400);
        }

        if (claims.FacilityId != request.FacilityId)
            return Result<ScanGateResponse>.Failure("This pass belongs to a different facility.", 403);

        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        var booking = await context.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == claims.BookingId, cancellationToken);

        if (booking is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanGateResponse>.Failure("Booking not found.", 404);
        }

        // Signature alone is not enough: a rotated nonce means this exact QR was
        // invalidated after it was issued, even though the token itself is intact.
        if (booking.PassNonce != claims.PassNonce)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanGateResponse>.Failure("This pass has been replaced. Ask the rider to refresh their QR.", 409);
        }

        if (booking.FacilityId != request.FacilityId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanGateResponse>.Failure("This booking is not for this facility.", 403);
        }

        // The booking's own state picks the direction; the gate rules then decide
        // whether that direction is allowed right now.
        var direction = GateRules.DirectionFor(booking.Status);
        var outcome = direction == ScanTypeEnum.Exit
            ? GateRules.CheckExit(booking.Status)
            : GateRules.CheckEntry(
                booking.Status,
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                now,
                settings.EarlyEntryGraceMinutes,
                settings.LateEntryGraceMinutes);

        if (outcome != ScanOutcomeEnum.Accepted)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<ScanGateResponse>.Failure(
                direction == ScanTypeEnum.Exit
                    ? ExitRefusal(outcome, booking.Status)
                    : EntryRefusal(outcome),
                409);
        }

        if (direction == ScanTypeEnum.Exit)
        {
            booking.Complete(now, staffId.Value);

            // Priced only after Complete(), because that is what fills in
            // OverstayMinutes, and snapshotted onto the row in the same
            // transaction as the exit itself: the figure staff see, the figure
            // the rider is shown and the figure eventually charged are one
            // number, frozen here. Null means nothing is owed — on time, inside
            // the grace window, or a facility that prices the stay at zero.
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
            var finalOutcome = isOverstay ? ScanOutcomeEnum.Overstay : ScanOutcomeEnum.Accepted;

            var exitMessage = OverstayPricing.DescribeExitMessage(
                booking.OverstayMinutes,
                settings.OverstayGraceMinutes,
                charge);

            return Result<ScanGateResponse>.Success(
                new ScanGateResponse(
                    ScanTypeEnum.Exit,
                    finalOutcome,
                    finalOutcome.ToDescription(),
                    booking.Id,
                    booking.Vehicle?.VehicleNumber ?? string.Empty,
                    booking.VehicleType,
                    booking.VehicleType.ToDescription(),
                    booking.StartsAtUtc,
                    booking.EndsAtUtc,
                    booking.EnteredAtUtc,
                    booking.ExitedAtUtc,
                    booking.SpaceReleasedAtUtc,
                    booking.ActualStayMinutes,
                    booking.OverstayMinutes,
                    settings.EarlyEntryGraceMinutes,
                    settings.OverstayGraceMinutes,
                    charge is not null,
                    charge?.BillableHours,
                    charge?.AmountPaisa,
                    now,
                    exitMessage));
        }

        booking.Activate(now, staffId.Value);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ScanGateResponse>.Success(
            new ScanGateResponse(
                ScanTypeEnum.Entry,
                ScanOutcomeEnum.Accepted,
                ScanOutcomeEnum.Accepted.ToDescription(),
                booking.Id,
                booking.Vehicle?.VehicleNumber ?? string.Empty,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.EnteredAtUtc,
                null,
                null,
                null,
                0,
                settings.EarlyEntryGraceMinutes,
                settings.OverstayGraceMinutes,
                false,
                null,
                null,
                now,
                "Entry recorded."));
    }

    private static string EntryRefusal(ScanOutcomeEnum outcome)
        => outcome switch
        {
            ScanOutcomeEnum.TooEarly => "It is too early to enter. The rider's window has not opened.",
            ScanOutcomeEnum.TooLate => "This pass has expired. The rider must book again.",
            ScanOutcomeEnum.NotPaid => "This booking has not been paid.",
            _ => "This booking cannot be used to enter."
        };

    private static string ExitRefusal(ScanOutcomeEnum outcome, BookingStatusEnum status)
        => outcome switch
        {
            ScanOutcomeEnum.AlreadyClosed => "This booking is already closed out.",
            ScanOutcomeEnum.NotCheckedIn when status == BookingStatusEnum.PendingPayment =>
                "This booking has not been paid.",
            _ => "No entry scan was recorded for this booking, so there is no stay to close."
        };
}
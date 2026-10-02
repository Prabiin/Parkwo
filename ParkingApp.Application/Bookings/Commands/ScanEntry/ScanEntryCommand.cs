using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.ScanEntry;

/// <summary>
/// Gate entry scan. Starts the measured stay: the moment the vehicle is
/// admitted becomes the billing clock, not the booking's start time.
/// <para>
/// The scan is a SERIALIZABLE transaction because two gates (or one gate
/// double-tapping) can read "Confirmed" at the same instant and both admit the
/// vehicle, double-counting the stay.
/// </para>
/// </summary>
public sealed record ScanEntryCommand(
    string PassToken,
    Guid FacilityId)
    : IRequestResult<ScanEntryCommand, ScanEntryResponse>;

public sealed record ScanEntryResponse(
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

public sealed class ScanEntryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IOptions<PassSettings> passSettings,
    ParkingPassService passes)
    : IRequestResultHandler<ScanEntryCommand, ScanEntryResponse>
{
    public async Task<Result<ScanEntryResponse>> Handle(
        ScanEntryCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.UserId;
        if (staffId is null)
            return Result<ScanEntryResponse>.Failure("Authentication required.", 401);

        if (!passes.IsConfigured)
            return Result<ScanEntryResponse>.Failure(ParkingPassService.NotConfiguredMessage, 503);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<ScanEntryResponse>.Failure("You are not authorised to scan passes at this facility.", 403);

        // The server's clock is the billing clock. A scanner with a skewed clock
        // must not be able to shorten a stay.
        var now = DateTimeOffset.UtcNow;
        var settings = passSettings.Value;

        // Signature and shape only. The time window is deliberately NOT applied
        // here: GateRules.CheckEntry below decides, using the booking's real
        // window, so a too-early rider gets "your window has not opened" instead
        // of a generic rejection. Verify() is used by the pass query instead.
        var claims = passes.VerifySignatureOnly(request.PassToken);
        if (claims is null)
        {
            // Distinguish "expired, rebook" from "forged, call support" — the
            // rider's next step is completely different in each case.
            return passes.IsExpired(request.PassToken, now)
                ? Result<ScanEntryResponse>.Failure("This pass has expired. The rider must book again.", 409)
                : Result<ScanEntryResponse>.Failure("This pass is not valid.", 400);
        }

        if (claims.FacilityId != request.FacilityId)
            return Result<ScanEntryResponse>.Failure("This pass belongs to a different facility.", 403);

        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        var booking = await context.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == claims.BookingId, cancellationToken);

        if (booking is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanEntryResponse>.Failure("Booking not found.", 404);
        }

        // Signature alone is not enough: a rotated nonce means this exact QR was
        // invalidated after it was issued, even though the token itself is intact.
        if (booking.PassNonce != claims.PassNonce)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanEntryResponse>.Failure("This pass has been replaced. Ask the rider to refresh their QR.", 409);
        }

        if (booking.FacilityId != request.FacilityId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<ScanEntryResponse>.Failure("This booking is not for this facility.", 403);
        }

        var gateOutcome = GateRules.CheckEntry(
            booking.Status,
            booking.StartsAtUtc,
            booking.EndsAtUtc,
            now,
            settings.EarlyEntryGraceMinutes,
            settings.LateEntryGraceMinutes);

        if (gateOutcome != ScanOutcomeEnum.Accepted)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<ScanEntryResponse>.Failure(EntryRefusal(gateOutcome), 409);
        }

        booking.Activate(now, staffId.Value);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ScanEntryResponse>.Success(
            new ScanEntryResponse(
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
                0,
                now,
                "Entry recorded."));
    }

    private static string EntryRefusal(ScanOutcomeEnum outcome)
        => outcome switch
        {
            ScanOutcomeEnum.TooEarly => "It is too early to enter. The rider's window has not opened.",
            ScanOutcomeEnum.AlreadyParked => "This vehicle is already parked.",
            ScanOutcomeEnum.NotPaid => "This booking has not been paid.",
            _ => "This booking cannot be used to enter."
        };
}

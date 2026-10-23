using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.CreateWalkIn;

/// <summary>
/// Opens an unpaid walk-in hold on a gate staff member's device: a rider at the
/// barrier with no Parkwo account. The same serializable availability gate as an
/// app booking applies, so a walk-in and an app booking can never double-sell
/// the last space. Identity is captured as snapshots (name, phone, plate) rather
/// than a User/Vehicle row — nothing the rider volunteers is linked to a real
/// account. Name, phone and plate are all required: the phone is the exit
/// fallback key, and the plate is what staff match the vehicle against when the
/// rider's QR will not scan.
/// </summary>
public sealed record CreateWalkInBookingCommand(
    Guid FacilityId,
    VehicleTypeEnum VehicleType,
    int Hours,
    string RiderName,
    string PhoneNumber,
    string PlateNumber)
    : IRequestResult<CreateWalkInBookingCommand, CreateWalkInBookingResponse>;

public sealed record CreateWalkInBookingResponse(
    Guid BookingId,
    BookingStatusEnum Status,
    string StatusDescription,
    Guid FacilityId,
    string FacilityName,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    string RiderName,
    string PhoneNumber,
    string PlateNumber,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal PricePerHourNpr,
    int BillableHours,
    decimal TotalAmountNpr,
    long AmountPaisa,
    DateTimeOffset HoldExpiresAtUtc);

public sealed class CreateWalkInBookingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IOptions<ParkwoPricingSettings> pricingSettings)
    : IRequestResultHandler<CreateWalkInBookingCommand, CreateWalkInBookingResponse>
{
    private const int HoldMinutes = 10;

    public async Task<Result<CreateWalkInBookingResponse>> Handle(
        CreateWalkInBookingCommand request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<CreateWalkInBookingResponse>.Failure("Authentication required.", 401);

        if (!await GateAccess.CanScanAsync(context, staffId.Value, request.FacilityId, cancellationToken))
            return Result<CreateWalkInBookingResponse>.Failure(
                "You are not authorised to open walk-in bookings at this facility.", 403);

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<CreateWalkInBookingResponse>.Failure("Parking facility not found.", 404);

        if (facility.ApprovalStatus != ApprovalStatusEnum.Verified)
            return Result<CreateWalkInBookingResponse>.Failure("This facility is not open for parking.", 403);

        var capacity = request.VehicleType == VehicleTypeEnum.TwoWheeler
            ? facility.TwoWheelerOccupancy
            : facility.FourWheelerOccupancy;

        if (capacity < 1)
            return Result<CreateWalkInBookingResponse>.Failure(
                $"This facility has no {request.VehicleType.ToDescription().ToLowerInvariant()} spaces.", 409);

        // A walk-in is already at the gate, so the window opens now and runs for
        // the whole hours the rider pays for. Billable hours still come from the
        // shared rounding rule, so walk-in and app pricing cannot drift.
        var now = DateTimeOffset.UtcNow;
        var startsAt = now;
        var endsAt = now.AddHours(request.Hours);
        var billableHours = BookingPricing.BillableHours(endsAt - startsAt);

        if (billableHours == 0)
            return Result<CreateWalkInBookingResponse>.Failure("A walk-in must be for at least one hour.", 400);

        // Parkwo sets the rate; the facility has no price of its own. Snapshot
        // the configured rate onto the booking so a later rate change never
        // reprices this stay.
        var pricePerHour = request.VehicleType == VehicleTypeEnum.TwoWheeler
            ? pricingSettings.Value.TwoWheelerPricePerHourNpr
            : pricingSettings.Value.FourWheelerPricePerHourNpr;

        var phone = PhoneNumbers.Normalize(request.PhoneNumber);

        // SERIALIZABLE: the availability count and the insert must not interleave
        // with a competing booking (app or walk-in) for the same lot.
        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        var occupying = await context.Bookings
            .AsNoTracking()
            .Where(b => b.FacilityId == facility.Id
                        && b.VehicleType == request.VehicleType
                        && BookingAvailability.OccupyingStatuses.Contains(b.Status)
                        && b.StartsAtUtc < endsAt
                        && b.EndsAtUtc > startsAt)
            .Select(b => new
            {
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc,
                b.StartsAtUtc,
                b.EndsAtUtc
            })
            .ToListAsync(cancellationToken);

        var liveCount = occupying.Count(b =>
            BookingAvailability.ConsumesCapacity(
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc,
                now)
            && BookingAvailability.Overlaps(
                b.StartsAtUtc,
                BookingAvailability.EffectiveEnd(b.EndsAtUtc, b.SpaceReleasedAtUtc),
                startsAt,
                endsAt));

        if (BookingAvailability.Available(capacity, liveCount) < 1)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<CreateWalkInBookingResponse>.Failure(
                $"No {request.VehicleType.ToDescription().ToLowerInvariant()} spaces are free.", 409);
        }

        var booking = Booking.CreateWalkIn(
            facility.Id,
            request.VehicleType,
            startsAt,
            endsAt,
            pricePerHour,
            billableHours,
            0m,
            HoldMinutes,
            request.RiderName,
            phone,
            request.PlateNumber,
            staffId.Value);

        context.Bookings.Add(booking);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<CreateWalkInBookingResponse>.Success(
            new CreateWalkInBookingResponse(
                booking.Id,
                booking.Status,
                booking.Status.ToDescription(),
                booking.FacilityId,
                facility.Name,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.WalkInRiderName ?? string.Empty,
                booking.WalkInPhoneNumber ?? string.Empty,
                booking.WalkInPlateNumber ?? string.Empty,
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.PricePerHourNpr,
                booking.BillableHours,
                booking.TotalAmountNpr,
                BookingPricing.ToPaisa(booking.TotalAmountNpr),
                booking.HoldExpiresAtUtc));
    }
}

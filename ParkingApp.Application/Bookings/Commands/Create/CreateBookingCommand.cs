using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Commands.Create;

/// <summary>
/// Creates an unpaid hold. The booking exists in PendingPayment and consumes
/// capacity only until HoldExpiresAtUtc, so an abandoned checkout releases the
/// space automatically. Availability is counted inside a serializable
/// transaction — two riders tapping "pay" at the same instant must not both
/// win the last space.
/// </summary>
public sealed record CreateBookingCommand(
    Guid FacilityId,
    Guid VehicleId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc)
    : IRequestResult<CreateBookingCommand, CreateBookingResponse>;

public sealed class CreateBookingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<CreateBookingCommand, CreateBookingResponse>
{
    private const int HoldMinutes = 10;

    public async Task<Result<CreateBookingResponse>> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreateBookingResponse>.Failure("Authentication required.", 401);

        if (request.EndsAtUtc <= request.StartsAtUtc)
            return Result<CreateBookingResponse>.Failure("Booking end time must be after the start time.", 400);

        var now = DateTimeOffset.UtcNow;

        if (request.StartsAtUtc < now.AddMinutes(-5))
            return Result<CreateBookingResponse>.Failure("Booking start time is in the past.", 400);

        var window = request.EndsAtUtc - request.StartsAtUtc;

        var billableHours = BookingPricing.BillableHours(window);
        if (billableHours == 0)
            return Result<CreateBookingResponse>.Failure("Booking window must be at least one hour.", 400);

        var vehicle = await context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (vehicle is null)
            return Result<CreateBookingResponse>.Failure("Vehicle not found.", 404);

        if (vehicle.UserId != userId.Value)
            return Result<CreateBookingResponse>.Failure("You can only book with a vehicle on your own account.", 403);

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null)
            return Result<CreateBookingResponse>.Failure("Parking facility not found.", 404);

        if (facility.ApprovalStatus != ApprovalStatusEnum.Verified)
            return Result<CreateBookingResponse>.Failure("This facility is not open for booking.", 403);

        var capacity = vehicle.VehicleType == VehicleTypeEnum.TwoWheeler
            ? facility.TwoWheelerOccupancy
            : facility.FourWheelerOccupancy;

        var pricePerHour = vehicle.VehicleType == VehicleTypeEnum.TwoWheeler
            ? facility.TwoWheelerPricePerHourNpr
            : facility.FourWheelerPricePerHourNpr;

        if (capacity < 1)
            return Result<CreateBookingResponse>.Failure($"This facility has no {vehicle.VehicleType.ToDescription().ToLowerInvariant()} spaces.", 409);

        var licenseGate = LicenseGate.Check(
            await context.DrivingLicenses.AsNoTracking()
                .Where(d => d.UserId == userId)
                .ToListAsync(cancellationToken),
            vehicle.VehicleCategory);

        if (licenseGate is not null)
            return Result<CreateBookingResponse>.Failure(licenseGate, 403);

        // SERIALIZABLE: the availability count and the insert must not interleave
        // with a competing booking for the same lot, or both reads see one free
        // space and both write.
        await using var transaction = await context.BeginSerializableTransactionAsync(cancellationToken);

        // An early exit frees the space immediately, so a booking that was
        // released at 11:00 must not keep blocking a slot for the rest of a
        // window running to 14:00. That is what the case expression below does:
        // it compares against the effective end, not the raw EndsAtUtc.
        var occupying = await context.Bookings
            .AsNoTracking()
            .Where(b => b.FacilityId == facility.Id
                        && b.VehicleType == vehicle.VehicleType
                        && BookingAvailability.OccupyingStatuses.Contains(b.Status)
                        && b.StartsAtUtc < request.EndsAtUtc
                        && b.EndsAtUtc > request.StartsAtUtc)
            .Select(b => new
            {
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc,
                b.StartsAtUtc,
                b.EndsAtUtc
            })
            .ToListAsync(cancellationToken);

        // Counted in memory because an early exit shortens the occupied window:
        // the SQL predicate above deliberately over-fetches (it still matches on
        // the raw window) and the real end is computed per row.
        var liveCount = occupying.Count(b =>
            BookingAvailability.ConsumesCapacity(
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc,
                now)
            && BookingAvailability.Overlaps(
                b.StartsAtUtc,
                BookingAvailability.EffectiveEnd(b.EndsAtUtc, b.SpaceReleasedAtUtc),
                request.StartsAtUtc,
                request.EndsAtUtc));

        var available = BookingAvailability.Available(capacity, liveCount);

        if (available < 1)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<CreateBookingResponse>.Failure(
                $"No {vehicle.VehicleType.ToDescription().ToLowerInvariant()} spaces are free for that time window.", 409);
        }

        var booking = Booking.Create(
            userId.Value,
            facility.Id,
            vehicle.Id,
            vehicle.VehicleType,
            request.StartsAtUtc,
            request.EndsAtUtc,
            pricePerHour,
            billableHours,
            PlatformFeeCalculator.Calculate(BookingPricing.TotalNpr(pricePerHour, billableHours)),
            HoldMinutes);

        context.Bookings.Add(booking);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<CreateBookingResponse>.Success(
            new CreateBookingResponse(
                booking.Id,
                booking.Status,
                booking.Status.ToDescription(),
                booking.FacilityId,
                facility.Name,
                booking.VehicleId,
                vehicle.VehicleNumber,
                booking.VehicleType,
                booking.VehicleType.ToDescription(),
                booking.StartsAtUtc,
                booking.EndsAtUtc,
                booking.PricePerHourNpr,
                booking.BillableHours,
                booking.TotalAmountNpr,
                BookingPricing.ToPaisa(booking.TotalAmountNpr),
                booking.PlatformFeeNpr,
                booking.ProviderAmountNpr,
                booking.HoldExpiresAtUtc));
    }
}
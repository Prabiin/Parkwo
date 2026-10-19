using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetFacilityAvailability;

/// <summary>
/// Rider availability grid — "which hours can I book here?".
/// <para>
/// Two ways to ask:
/// <list type="bullet">
/// <item>a local <see cref="Date"/> (defaults to today): the hourly slots of
/// that day, today starting at the next local hour boundary;</item>
/// <item>an exact <see cref="FromUtc"/>/<see cref="ToUtc"/>: a single slot for
/// that window.</item>
/// </list>
/// The exact window matters when a rider selects several green hours: each hour
/// can have a space free while the combined window has none, so the selection
/// must be re-checked as one window before it is submitted.
/// </para>
/// Counts come from the same overlap rule as the booking gate.
/// </summary>
public sealed record GetFacilityAvailabilityQuery(
    Guid FacilityId,
    VehicleTypeEnum VehicleType,
    DateOnly? Date,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc)
    : IRequestResult<GetFacilityAvailabilityQuery, GetFacilityAvailabilityResponse>;

public sealed class GetFacilityAvailabilityQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetFacilityAvailabilityQuery, GetFacilityAvailabilityResponse>
{
    public async Task<Result<GetFacilityAvailabilityResponse>> Handle(
        GetFacilityAvailabilityQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetFacilityAvailabilityResponse>.Failure("Authentication required.", 401);

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Where(f => f.Id == request.FacilityId)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.ApprovalStatus,
                f.TwoWheelerOccupancy,
                f.FourWheelerOccupancy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (facility is null)
            return Result<GetFacilityAvailabilityResponse>.Failure("Parking facility not found.", 404);

        // Only Verified paid lots are bookable, so only they have a grid.
        if (facility.ApprovalStatus != ApprovalStatusEnum.Verified)
            return Result<GetFacilityAvailabilityResponse>.Failure("This facility is not open for booking.", 409);

        var capacity = request.VehicleType == VehicleTypeEnum.TwoWheeler
            ? facility.TwoWheelerOccupancy
            : facility.FourWheelerOccupancy;

        if (capacity < 1)
            return Result<GetFacilityAvailabilityResponse>.Failure(
                $"This facility has no {request.VehicleType.ToDescription().ToLowerInvariant()} spaces.", 409);

        var now = DateTimeOffset.UtcNow;

        DateOnly? day = null;
        IReadOnlyList<AvailabilityWindow> slots;

        if (request.FromUtc is { } from && request.ToUtc is { } to)
        {
            slots = [new AvailabilityWindow(from, to)];
        }
        else
        {
            day = request.Date ?? LocalTime.Today();
            slots = AvailabilitySlots.ForLocalDay(day.Value, now);
        }

        // One read covers every slot: a day is 24 hours and a facility's live
        // bookings are few. An interval that spans several slots is fetched once
        // and counted against each one.
        var intervals = new List<FacilityAvailability.OccupiedInterval>();

        if (slots.Count > 0)
        {
            intervals = (await FacilityAvailability.LoadOccupiedIntervalsAsync(
                context,
                [facility.Id],
                new AvailabilityWindow(slots[0].FromUtc, slots[^1].ToUtc),
                now,
                cancellationToken)).ToList();
        }

        var slotItems = slots
            .Select(slot =>
            {
                var taken = FacilityAvailability.CountOverlapping(
                    intervals, facility.Id, request.VehicleType, slot);
                var available = BookingAvailability.Available(capacity, taken);

                // Red means "no space left", not "someone has one": a 12-space
                // lot with 5 taken is still green.
                return new AvailabilitySlotResponse(
                    slot.FromUtc,
                    slot.ToUtc,
                    LocalTime.FormatTime(slot.FromUtc),
                    LocalTime.FormatTime(slot.ToUtc),
                    available,
                    available == 0);
            })
            .ToList();

        return Result<GetFacilityAvailabilityResponse>.Success(
            new GetFacilityAvailabilityResponse(
                facility.Id,
                facility.Name,
                day,
                request.VehicleType,
                request.VehicleType.ToDescription(),
                capacity,
                slotItems));
    }
}

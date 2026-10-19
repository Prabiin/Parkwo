using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// One place that answers "how many spaces are taken at these facilities for
/// this window", so every availability feed — rider nearby, rider availability
/// grid, owner list and detail — reports the same number from the same rule. A
/// space is taken when a live booking overlaps the window: a paid stay (in
/// progress or still to come) or an unexpired checkout hold. Both tests are the
/// ones <c>CreateBookingCommand</c> applies, so a feed can never advertise a
/// space the booking gate would refuse (409).
/// </summary>
public static class FacilityAvailability
{
    /// <summary>A live booking's occupied interval, shortened by an early exit.</summary>
    public readonly record struct OccupiedInterval(
        Guid FacilityId,
        VehicleTypeEnum VehicleType,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc);

    /// <summary>
    /// The live bookings of these facilities that overlap
    /// <paramref name="window"/>, as intervals. SQL pre-filters on the raw
    /// window; the real end is computed per row afterwards, because an early
    /// exit shortens the occupied window (see
    /// <see cref="BookingAvailability.EffectiveEnd"/>) and an expired hold frees
    /// the space lazily.
    /// </summary>
    public static async Task<IReadOnlyList<OccupiedInterval>> LoadOccupiedIntervalsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<Guid> facilityIds,
        AvailabilityWindow window,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (facilityIds.Count == 0)
            return [];

        var ids = facilityIds as List<Guid> ?? facilityIds.ToList();

        var rows = await context.Bookings
            .AsNoTracking()
            .Where(b => ids.Contains(b.FacilityId)
                        && BookingAvailability.OccupyingStatuses.Contains(b.Status)
                        && b.StartsAtUtc < window.ToUtc
                        && b.EndsAtUtc > window.FromUtc)
            .Select(b => new
            {
                b.FacilityId,
                b.VehicleType,
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc,
                b.StartsAtUtc,
                b.EndsAtUtc
            })
            .ToListAsync(cancellationToken);

        // Counted in memory, not in SQL: an early exit and a lapsed hold release
        // the space lazily — see BookingAvailability.ConsumesCapacity.
        var intervals = new List<OccupiedInterval>(rows.Count);

        foreach (var b in rows)
        {
            if (!BookingAvailability.ConsumesCapacity(
                    b.Status, b.HoldExpiresAtUtc, b.SpaceReleasedAtUtc, nowUtc))
                continue;

            var end = BookingAvailability.EffectiveEnd(b.EndsAtUtc, b.SpaceReleasedAtUtc);

            if (!BookingAvailability.Overlaps(b.StartsAtUtc, end, window.FromUtc, window.ToUtc))
                continue;

            intervals.Add(new OccupiedInterval(b.FacilityId, b.VehicleType, b.StartsAtUtc, end));
        }

        return intervals;
    }

    /// <summary>
    /// Spaces taken per facility and vehicle type for the window. Only
    /// facilities in <paramref name="facilityIds"/> are counted; callers pass the
    /// page they are about to render.
    /// </summary>
    public static async Task<Dictionary<(Guid FacilityId, VehicleTypeEnum VehicleType), int>>
        LoadTakenSpacesAsync(
            IApplicationDbContext context,
            IReadOnlyCollection<Guid> facilityIds,
            AvailabilityWindow window,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
    {
        var intervals = await LoadOccupiedIntervalsAsync(
            context, facilityIds, window, nowUtc, cancellationToken);

        return intervals
            .GroupBy(i => (i.FacilityId, i.VehicleType))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// How many of <paramref name="intervals"/> cover a slot — the raw figure
    /// each cell of the availability grid is built from.
    /// </summary>
    public static int CountOverlapping(
        IReadOnlyList<OccupiedInterval> intervals,
        Guid facilityId,
        VehicleTypeEnum vehicleType,
        AvailabilityWindow slot)
        => intervals.Count(i => i.FacilityId == facilityId
                                && i.VehicleType == vehicleType
                                && BookingAvailability.Overlaps(i.StartUtc, i.EndUtc, slot.FromUtc, slot.ToUtc));

    /// <summary>
    /// Whether a facility offers a space of that vehicle type at all. Used to
    /// keep a car-only lot out of a bike rider's world and vice versa: the pin
    /// feed filters on it, and the "Book a spot" screen drops vehicles the lot
    /// cannot take, so no screen ever has to grey out an unsupported type.
    /// </summary>
    public static bool ServesType(
        int twoWheelerOccupancy,
        int fourWheelerOccupancy,
        VehicleTypeEnum vehicleType)
        => vehicleType == VehicleTypeEnum.TwoWheeler
            ? twoWheelerOccupancy > 0
            : fourWheelerOccupancy > 0;

    /// <summary>
    /// Occupancy minus the spaces taken for that facility and type, clamped at
    /// zero. Reads 0 taken when the facility has no live bookings yet.
    /// </summary>
    public static int Available(
        int occupancy,
        IReadOnlyDictionary<(Guid FacilityId, VehicleTypeEnum VehicleType), int> takenSpaces,
        Guid facilityId,
        VehicleTypeEnum vehicleType)
        => BookingAvailability.Available(
            occupancy, takenSpaces.GetValueOrDefault((facilityId, vehicleType)));
}

using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// One place that answers "how many spaces are taken at these facilities right
/// now", so every availability feed — rider nearby, owner list and detail —
/// reports the same number from the same rule. A space is taken by any live
/// booking: a paid stay in progress or still to come, or an unexpired checkout
/// hold. This is deliberately the set <c>CreateBookingCommand</c> counts, so a
/// feed can never advertise a space the booking gate would refuse (409).
/// </summary>
public static class FacilityAvailability
{
    /// <summary>
    /// Spaces currently taken per facility and vehicle type. Only facilities in
    /// <paramref name="facilityIds"/> are counted; callers pass the page they are
    /// about to render.
    /// </summary>
    public static async Task<Dictionary<(Guid FacilityId, VehicleTypeEnum VehicleType), int>>
        LoadTakenSpacesAsync(
            IApplicationDbContext context,
            IReadOnlyCollection<Guid> facilityIds,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
    {
        if (facilityIds.Count == 0)
            return [];

        var ids = facilityIds as List<Guid> ?? facilityIds.ToList();

        var rows = await context.Bookings
            .AsNoTracking()
            .Where(b => ids.Contains(b.FacilityId)
                        && BookingAvailability.OccupyingStatuses.Contains(b.Status))
            .Select(b => new
            {
                b.FacilityId,
                b.VehicleType,
                b.Status,
                b.HoldExpiresAtUtc,
                b.SpaceReleasedAtUtc
            })
            .ToListAsync(cancellationToken);

        // Counted in memory, not in SQL: an early exit and a lapsed hold release
        // the space lazily — see BookingAvailability.ConsumesCapacity.
        return rows
            .Where(b => BookingAvailability.ConsumesCapacity(
                b.Status, b.HoldExpiresAtUtc, b.SpaceReleasedAtUtc, nowUtc))
            .GroupBy(b => (b.FacilityId, b.VehicleType))
            .ToDictionary(g => g.Key, g => g.Count());
    }

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

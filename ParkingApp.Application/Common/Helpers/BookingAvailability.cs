using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Window-overlap math for capacity checks. A booking holds one space of its
/// vehicle type for its whole window, so two bookings collide when their
/// intervals intersect (half-open: touching endpoints do not overlap).
/// </summary>
public static class BookingAvailability
{
    /// <summary>Statuses that still consume capacity.</summary>
    public static readonly BookingStatusEnum[] OccupyingStatuses =
    [
        BookingStatusEnum.PendingPayment,
        BookingStatusEnum.Confirmed,
        BookingStatusEnum.Active
    ];

    /// <summary>
    /// The point in time a booking stops holding its space. Normally the end of
    /// the paid window, but an exit scan ends it early: a rider who leaves at
    /// 11:00 for a window running to 14:00 frees that space for someone else at
    /// 11:00, not at 14:00. Without this the lot stays artificially full for
    /// hours after everyone has left.
    /// </summary>
    public static DateTimeOffset EffectiveEnd(
        DateTimeOffset endsAtUtc,
        DateTimeOffset? spaceReleasedAtUtc)
        => spaceReleasedAtUtc is { } released && released < endsAtUtc
            ? released
            : endsAtUtc;

    /// <summary>
    /// True when the two windows share any time. [startA, endA) vs [startB, endB).
    /// </summary>
    public static bool Overlaps(
        DateTimeOffset startA, DateTimeOffset endA,
        DateTimeOffset startB, DateTimeOffset endB)
        => startA < endB && startB < endA;

    /// <summary>
    /// Spaces left of <paramref name="capacity"/> after counting overlapping
    /// live bookings. Never returns a negative number.
    /// </summary>
    public static int Available(int capacity, int overlappingLiveBookings)
        => Math.Max(0, capacity - overlappingLiveBookings);

    /// <summary>
    /// Whether a row still holds a space. Expired pending holds release
    /// capacity lazily, so a crashed sweeper cannot sell the same space twice
    /// and availability is correct even before the sweeper runs.
    /// </summary>
    public static bool ConsumesCapacity(
        BookingStatusEnum status,
        DateTimeOffset holdExpiresAtUtc,
        DateTimeOffset nowUtc)
    {
        if (!OccupyingStatuses.Contains(status))
            return false;

        return status != BookingStatusEnum.PendingPayment
               || holdExpiresAtUtc > nowUtc;
    }

    /// <summary>
    /// Whether a booking still occupies a space at <paramref name="nowUtc"/>,
    /// taking the real release time into account. Callers must overlap against
    /// <see cref="EffectiveEnd"/>, not the raw window end, or an early exit will
    /// keep blocking the space it already freed.
    /// </summary>
    public static bool ConsumesCapacity(
        BookingStatusEnum status,
        DateTimeOffset holdExpiresAtUtc,
        DateTimeOffset? spaceReleasedAtUtc,
        DateTimeOffset nowUtc)
    {
        if (spaceReleasedAtUtc is { } released && released <= nowUtc)
            return false;

        return ConsumesCapacity(status, holdExpiresAtUtc, nowUtc);
    }
}
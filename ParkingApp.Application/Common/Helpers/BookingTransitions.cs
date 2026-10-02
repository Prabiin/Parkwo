using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Legal booking transitions. Prepaid makes this tighter than it looks:
/// once payment has settled the only forward move is into use, and anything
/// that gives money back must pass through Refunded so the ledger stays honest.
/// </summary>
public static class BookingTransitions
{
    public static bool IsAllowed(BookingStatusEnum from, BookingStatusEnum to)
    {
        if (from == to)
            return false;

        return from switch
        {
            // Unpaid holds can die quietly (TTL) or be abandoned by the rider.
            BookingStatusEnum.PendingPayment =>
                to is BookingStatusEnum.Confirmed
                    or BookingStatusEnum.Cancelled
                    or BookingStatusEnum.Expired,

            // Paid but not yet used: rider cancels (refund path decides money),
            // or shows up (entry scan), or the window elapses unused.
            BookingStatusEnum.Confirmed =>
                to is BookingStatusEnum.Active
                    or BookingStatusEnum.Completed
                    or BookingStatusEnum.Cancelled
                    or BookingStatusEnum.Refunded,

            // Parked. Overstay handling extends or penalises here.
            BookingStatusEnum.Active =>
                to is BookingStatusEnum.Completed,

            // Terminal.
            BookingStatusEnum.Completed => false,
            BookingStatusEnum.Cancelled => false,
            BookingStatusEnum.Expired => false,

            // A refunded booking that was never used can be reinstated.
            BookingStatusEnum.Refunded =>
                to is BookingStatusEnum.Cancelled,

            _ => false
        };
    }
}
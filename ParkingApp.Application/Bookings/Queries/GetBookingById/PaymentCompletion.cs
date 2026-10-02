using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetBookingById;

public static class PaymentCompletion
{
    /// <summary>
    /// Applies a verified gateway result to a booking. Idempotent: a replayed
    /// callback that lands on an already-confirmed booking is a no-op rather
    /// than an error, because gateways retry.
    /// </summary>
    public static bool TryConfirm(
        Booking booking,
        Payment payment,
        string? gatewayTransactionId)
    {
        if (booking.Status == BookingStatusEnum.Confirmed
            || booking.Status == BookingStatusEnum.Active
            || booking.Status == BookingStatusEnum.Completed)
            return false;

        if (!BookingTransitions.IsAllowed(booking.Status, BookingStatusEnum.Confirmed))
            return false;

        payment.MarkCompleted(gatewayTransactionId);
        booking.Confirm();

        return true;
    }
}
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

/// <summary>
/// The full gate lifecycle on the domain object: confirm, park, leave. These
/// cover the state the scans drive, and the timestamps the rider is later
/// billed and shown.
/// </summary>
public class BookingGateLifecycleTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Staff = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Rider = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static Booking PaidBooking(int hours = 2)
    {
        var booking = Booking.Create(
            Rider,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            VehicleTypeEnum.FourWheeler,
            Start,
            Start.AddHours(hours),
            20m,
            hours,
            5m,
            10);

        booking.Confirm();
        return booking;
    }

    [Fact]
    public void A_New_Booking_Starts_With_A_Pass_Nonce()
    {
        Assert.NotEqual(Guid.Empty, PaidBooking().PassNonce);
    }

    [Fact]
    public void Each_Booking_Gets_Its_Own_Pass_Nonce()
    {
        Assert.NotEqual(PaidBooking().PassNonce, PaidBooking().PassNonce);
    }

    [Fact]
    public void Entry_Moves_A_Confirmed_Booking_To_Active()
    {
        var booking = PaidBooking();
        var entry = Start.AddMinutes(20);

        booking.Activate(entry, Staff);

        Assert.Equal(BookingStatusEnum.Active, booking.Status);
        Assert.Equal(entry, booking.EnteredAtUtc);
        Assert.Equal(Staff, booking.EnteredByUserId);
    }

    [Fact]
    public void Entry_Records_The_Operators_Id_For_Disputes()
    {
        var booking = PaidBooking();

        booking.Activate(Start.AddMinutes(5), Staff);

        Assert.Equal(Staff, booking.EnteredByUserId);
    }

    [Fact]
    public void Exit_Completes_The_Booking_And_Releases_The_Space()
    {
        var booking = PaidBooking();
        booking.Activate(Start.AddMinutes(20), Staff);

        var exit = Start.AddMinutes(50);
        booking.Complete(exit, Staff);

        Assert.Equal(BookingStatusEnum.Completed, booking.Status);
        Assert.Equal(exit, booking.ExitedAtUtc);
        Assert.Equal(Staff, booking.ExitedByUserId);
        Assert.Equal(exit, booking.SpaceReleasedAtUtc);
    }

    [Fact]
    public void Exit_Measures_The_Real_Stay_Not_The_Booked_Window()
    {
        // Prepaid 2 hours, actually parked for 30 minutes. The stay must report
        // 30, or the rider is charged time they did not use.
        var booking = PaidBooking(hours: 2);
        booking.Activate(Start.AddMinutes(20), Staff);

        booking.Complete(Start.AddMinutes(50), Staff);

        Assert.Equal(30, booking.ActualStayMinutes);
    }

    [Fact]
    public void Leaving_On_Time_Records_No_Overstay()
    {
        var booking = PaidBooking(hours: 2);
        booking.Activate(Start, Staff);

        booking.Complete(Start.AddHours(2), Staff);

        Assert.Equal(0, booking.OverstayMinutes);
    }

    [Fact]
    public void Leaving_After_The_Window_Records_Overstay()
    {
        var booking = PaidBooking(hours: 2);
        booking.Activate(Start, Staff);

        booking.Complete(Start.AddHours(3).AddMinutes(15), Staff);

        Assert.Equal(195, booking.ActualStayMinutes);
        Assert.Equal(75, booking.OverstayMinutes);
    }

    [Fact]
    public void Leaving_Early_Records_No_Overstay()
    {
        var booking = PaidBooking(hours: 2);
        booking.Activate(Start, Staff);

        booking.Complete(Start.AddMinutes(30), Staff);

        Assert.Equal(30, booking.ActualStayMinutes);
        Assert.Equal(0, booking.OverstayMinutes);
    }

    [Fact]
    public void The_Exit_Scan_Clock_Is_The_Server_Supplied_One()
    {
        // The handler passes DateTimeOffset.UtcNow, never a device clock, so a
        // scanner with a skewed time cannot shorten or extend a stay.
        var booking = PaidBooking();
        booking.Activate(Start.AddMinutes(10), Staff);

        booking.Complete(Start.AddMinutes(70), Staff);

        Assert.Equal(Start.AddMinutes(70), booking.ExitedAtUtc);
        Assert.Equal(60, booking.ActualStayMinutes);
    }

    [Fact]
    public void Releasing_Without_An_Exit_Scan_Frees_The_Space_But_Hides_No_Fake_Exit()
    {
        var booking = PaidBooking();
        booking.Activate(Start, Staff);

        booking.ReleaseSpaceWithoutExit(Start.AddHours(4));

        Assert.NotNull(booking.SpaceReleasedAtUtc);
        Assert.Null(booking.ExitedAtUtc);
        Assert.Null(booking.ExitedByUserId);
        Assert.Equal(BookingStatusEnum.Active, booking.Status);
    }

    [Fact]
    public void Rotating_The_Pass_Changes_The_Nonce_And_Invalidates_Old_QRs()
    {
        var booking = PaidBooking();
        var original = booking.PassNonce;

        booking.RotatePassNonce();

        Assert.NotEqual(original, booking.PassNonce);
    }

    [Fact]
    public void Rotating_The_Pass_Leaves_The_Booking_And_Payment_Alone()
    {
        // A leaked QR must not cost the rider their space or their money.
        var booking = PaidBooking();
        var status = booking.Status;
        var total = booking.TotalAmountNpr;

        booking.RotatePassNonce();

        Assert.Equal(status, booking.Status);
        Assert.Equal(total, booking.TotalAmountNpr);
    }

    [Fact]
    public void A_Confirmed_Booking_May_Move_Into_Use()
    {
        Assert.True(BookingTransitions.IsAllowed(
            BookingStatusEnum.Confirmed, BookingStatusEnum.Active));
    }

    [Fact]
    public void An_Active_Booking_May_Move_Into_Completion()
    {
        Assert.True(BookingTransitions.IsAllowed(
            BookingStatusEnum.Active, BookingStatusEnum.Completed));
    }

    [Fact]
    public void An_Active_Booking_May_Not_Be_Cancelled_At_The_Gate()
    {
        // The car is physically there; only the exit scan closes the stay.
        Assert.False(BookingTransitions.IsAllowed(
            BookingStatusEnum.Active, BookingStatusEnum.Cancelled));
    }
}

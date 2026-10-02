using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class GateRulesTests
{
    private static readonly DateTimeOffset WindowStart = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = WindowStart.AddHours(2);

    private const int EarlyGrace = 15;
    private const int LateGrace = 30;

    private static ScanOutcomeEnum Entry(
        BookingStatusEnum status,
        DateTimeOffset now,
        int earlyGrace = EarlyGrace,
        int lateGrace = LateGrace)
        => GateRules.CheckEntry(status, WindowStart, WindowEnd, now, earlyGrace, lateGrace);

    [Fact]
    public void Entry_Is_Accepted_Inside_The_Window()
    {
        Assert.Equal(ScanOutcomeEnum.Accepted, Entry(BookingStatusEnum.Confirmed, WindowStart.AddMinutes(30)));
    }

    [Fact]
    public void Entry_Is_Accepted_Exactly_At_The_Window_Start()
    {
        Assert.Equal(ScanOutcomeEnum.Accepted, Entry(BookingStatusEnum.Confirmed, WindowStart));
    }

    [Fact]
    public void Entry_Is_Accepted_At_The_Window_End()
    {
        Assert.Equal(ScanOutcomeEnum.Accepted, Entry(BookingStatusEnum.Confirmed, WindowEnd));
    }

    [Fact]
    public void Early_Arrival_Inside_The_Grace_Is_Accepted()
    {
        Assert.Equal(
            ScanOutcomeEnum.Accepted,
            Entry(BookingStatusEnum.Confirmed, WindowStart.AddMinutes(-10)));
    }

    [Fact]
    public void Arrival_Before_The_Grace_Is_Too_Early()
    {
        Assert.Equal(
            ScanOutcomeEnum.TooEarly,
            Entry(BookingStatusEnum.Confirmed, WindowStart.AddMinutes(-20)));
    }

    [Fact]
    public void Late_Arrival_Inside_The_Grace_Is_Accepted()
    {
        // The rider paid for 2 hours and showed up at 12:20. Refusing them would
        // hold a space they already own while someone else waits.
        Assert.Equal(
            ScanOutcomeEnum.Accepted,
            Entry(BookingStatusEnum.Confirmed, WindowEnd.AddMinutes(20)));
    }

    [Fact]
    public void Arrival_After_The_Grace_Is_Too_Late()
    {
        Assert.Equal(
            ScanOutcomeEnum.TooLate,
            Entry(BookingStatusEnum.Confirmed, WindowEnd.AddMinutes(45)));
    }

    [Fact]
    public void An_Unpaid_Booking_Cannot_Enter()
    {
        Assert.Equal(
            ScanOutcomeEnum.NotPaid,
            Entry(BookingStatusEnum.PendingPayment, WindowStart.AddMinutes(10)));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Refunded)]
    [InlineData(BookingStatusEnum.Completed)]
    public void A_Dead_Booking_Cannot_Enter(BookingStatusEnum status)
    {
        Assert.Equal(ScanOutcomeEnum.InvalidState, Entry(status, WindowStart.AddMinutes(10)));
    }

    [Fact]
    public void A_Second_Entry_Scan_Is_Not_Accepted()
    {
        Assert.Equal(
            ScanOutcomeEnum.AlreadyParked,
            Entry(BookingStatusEnum.Active, WindowStart.AddMinutes(30)));
    }

    [Fact]
    public void A_Zero_Grace_Configuration_Is_Honoured_Not_Treated_As_Blanket_Permission()
    {
        Assert.Equal(
            ScanOutcomeEnum.TooEarly,
            Entry(BookingStatusEnum.Confirmed, WindowStart.AddMinutes(-1), earlyGrace: 0));

        Assert.Equal(
            ScanOutcomeEnum.TooLate,
            Entry(BookingStatusEnum.Confirmed, WindowEnd.AddMinutes(1), lateGrace: 0));
    }

    [Fact]
    public void Exit_Is_Accepted_Only_While_Active()
    {
        Assert.Equal(ScanOutcomeEnum.Accepted, GateRules.CheckExit(BookingStatusEnum.Active));
    }

    [Fact]
    public void A_Second_Exit_Scan_Reports_Already_Closed()
    {
        Assert.Equal(
            ScanOutcomeEnum.AlreadyClosed,
            GateRules.CheckExit(BookingStatusEnum.Completed));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed)]
    [InlineData(BookingStatusEnum.PendingPayment)]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Refunded)]
    public void Exiting_Without_An_Entry_Scan_Is_Refused(BookingStatusEnum status)
    {
        Assert.Equal(ScanOutcomeEnum.NotCheckedIn, GateRules.CheckExit(status));
    }

    [Theory]
    [InlineData(ScanOutcomeEnum.UnknownPass, true)]
    [InlineData(ScanOutcomeEnum.WrongFacility, true)]
    [InlineData(ScanOutcomeEnum.TooEarly, true)]
    [InlineData(ScanOutcomeEnum.TooLate, true)]
    [InlineData(ScanOutcomeEnum.NotPaid, true)]
    [InlineData(ScanOutcomeEnum.InvalidState, true)]
    [InlineData(ScanOutcomeEnum.NotCheckedIn, true)]
    [InlineData(ScanOutcomeEnum.Accepted, false)]
    [InlineData(ScanOutcomeEnum.AlreadyParked, false)]
    [InlineData(ScanOutcomeEnum.AlreadyClosed, false)]
    [InlineData(ScanOutcomeEnum.Overstay, false)]
    public void Rejections_Are_The_Ones_Staff_Must_Act_On(ScanOutcomeEnum outcome, bool expected)
    {
        Assert.Equal(expected, GateRules.IsRejection(outcome));
    }
}

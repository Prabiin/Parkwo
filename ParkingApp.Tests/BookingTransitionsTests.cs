using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class BookingTransitionsTests
{
    [Theory]
    [InlineData(BookingStatusEnum.PendingPayment, BookingStatusEnum.Confirmed)]
    [InlineData(BookingStatusEnum.PendingPayment, BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.PendingPayment, BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.Active)]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.Refunded)]
    [InlineData(BookingStatusEnum.Active, BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Refunded, BookingStatusEnum.Cancelled)]
    public void IsAllowed_Accepts_Legal_Moves(BookingStatusEnum from, BookingStatusEnum to)
    {
        Assert.True(BookingTransitions.IsAllowed(from, to));
    }

    [Fact]
    public void IsAllowed_Rejects_Self_Transition()
    {
        Assert.False(BookingTransitions.IsAllowed(
            BookingStatusEnum.Confirmed, BookingStatusEnum.Confirmed));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Confirmed, BookingStatusEnum.PendingPayment)]
    [InlineData(BookingStatusEnum.Active, BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Active, BookingStatusEnum.Refunded)]
    [InlineData(BookingStatusEnum.PendingPayment, BookingStatusEnum.Completed)]
    public void IsAllowed_Rejects_Backwards_And_Skipped_Moves(BookingStatusEnum from, BookingStatusEnum to)
    {
        Assert.False(BookingTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    public void IsAllowed_Terminal_States_Are_Frozen(BookingStatusEnum status)
    {
        Assert.False(BookingTransitions.IsAllowed(status, BookingStatusEnum.Active));
        Assert.False(BookingTransitions.IsAllowed(status, BookingStatusEnum.Completed));
    }
}
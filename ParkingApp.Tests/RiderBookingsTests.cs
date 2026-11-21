using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.GetMyBookings;
using ParkingApp.Application.Bookings.Queries.Shared;
using ParkingApp.Application.SavedParkings.Commands.Remove;
using ParkingApp.Application.SavedParkings.Commands.Save;
using ParkingApp.Application.Vehicles.Commands.Create;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Tests;

/// <summary>
/// The rider's booking list, saved places and the simplified vehicle form.
/// The SQL-translation tests build each query against the real Npgsql
/// provider and ask for its SQL — no database is contacted — so a LINQ shape
/// Postgres cannot run (like the UpdateRole bug) fails here, not in production.
/// </summary>
public class RiderBookingsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 6, 0, 0, TimeSpan.Zero);

    private static ApplicationDbContext NewContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=never-opened", o => o.UseNetTopologySuite())
            .Options);

    private static RiderBookingRow Row(
        BookingStatusEnum status,
        DateTimeOffset? holdExpires = null,
        long? overstayPaisa = null,
        bool overstaySettled = false,
        decimal total = 80m) => new()
    {
        Id = Guid.NewGuid(),
        Status = status,
        FacilityId = Guid.NewGuid(),
        FacilityName = "Thamel Courtyard",
        FacilityAddress = "Jyatha Marg",
        VehicleNumber = "BA 12 PA 3456",
        VehicleType = VehicleTypeEnum.TwoWheeler,
        StartsAtUtc = Now.AddHours(1),
        EndsAtUtc = Now.AddHours(3),
        HoldExpiresAtUtc = holdExpires ?? Now.AddMinutes(5),
        TotalAmountNpr = total,
        OverstayAmountPaisa = overstayPaisa,
        OverstaySettled = overstaySettled
    };

    [Fact]
    public void A_lapsed_unpaid_hold_reads_as_expired_and_paid_nothing()
    {
        var item = RiderBookings.ToResponse(
            Row(BookingStatusEnum.PendingPayment, holdExpires: Now.AddMinutes(-1)), Now);

        Assert.Equal(BookingStatusEnum.Expired, item.Status);
        Assert.Equal(0m, item.TotalPaidNpr);
        Assert.Null(item.HoldExpiresAtUtc);
    }

    [Fact]
    public void A_live_unpaid_hold_exposes_its_expiry_for_the_countdown()
    {
        var item = RiderBookings.ToResponse(Row(BookingStatusEnum.PendingPayment), Now);

        Assert.Equal(BookingStatusEnum.PendingPayment, item.Status);
        Assert.Equal(Now.AddMinutes(5), item.HoldExpiresAtUtc);
    }

    [Fact]
    public void An_unpaid_overstay_is_due_and_not_counted_as_paid()
    {
        var item = RiderBookings.ToResponse(
            Row(BookingStatusEnum.Completed, overstayPaisa: 4000), Now);

        Assert.True(item.OverstayDue);
        Assert.Equal(40m, item.OverstayAmountNpr);
        Assert.Equal(80m, item.TotalPaidNpr);
    }

    [Fact]
    public void A_settled_overstay_adds_to_the_total_paid()
    {
        var item = RiderBookings.ToResponse(
            Row(BookingStatusEnum.Completed, overstayPaisa: 4000, overstaySettled: true), Now);

        Assert.False(item.OverstayDue);
        Assert.Equal(120m, item.TotalPaidNpr);
    }

    [Fact]
    public void Upcoming_and_history_queries_translate_to_sql()
    {
        using var context = NewContext();

        var upcoming = RiderBookings.Project(context,
            RiderBookings.Upcoming(context.Bookings, Now).OrderBy(b => b.StartsAtUtc)).ToQueryString();
        var history = RiderBookings.Project(context,
            RiderBookings.History(context.Bookings, Now).OrderByDescending(b => b.StartsAtUtc)).ToQueryString();

        Assert.Contains("FROM \"Bookings\"", upcoming);
        Assert.Contains("FROM \"Bookings\"", history);
    }

    [Fact]
    public void A_permission_lookup_by_materialised_ids_translates_to_sql()
    {
        // The shape UpdateRole now uses: a List<Guid>, not an entity collection.
        using var context = NewContext();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var sql = context.BackOfficeMenuActions
            .Where(a => ids.Contains(a.Id))
            .OrderBy(a => a.MenuKey)
            .Select(a => new { a.MenuKey, a.ActionKey })
            .ToQueryString();

        Assert.Contains("BackOfficeMenuActions", sql);
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 51, false)]
    public void Booking_list_paging_is_bounded(int page, int pageSize, bool valid)
    {
        var result = new GetMyBookingsQueryValidator()
            .Validate(new GetMyBookingsQuery(BookingListScopeEnum.History, page, pageSize));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void Saving_needs_exactly_one_target(bool facility, bool free, bool valid)
    {
        var result = new SaveParkingCommandValidator().Validate(new SaveParkingCommand(
            facility ? Guid.NewGuid() : null,
            free ? Guid.NewGuid() : null));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Removing_needs_exactly_one_target()
    {
        var validator = new RemoveSavedParkingCommandValidator();

        Assert.True(validator.Validate(new RemoveSavedParkingCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void A_vehicle_registers_with_type_only()
    {
        var result = new CreateVehicleCommandValidator().Validate(new CreateVehicleCommand(
            VehicleTypeEnum.TwoWheeler, "Daily ride", "BA 12 PA 3456", "Honda", "Dio", "Pearl white"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void A_category_that_contradicts_the_type_is_still_refused()
    {
        var result = new CreateVehicleCommandValidator().Validate(new CreateVehicleCommand(
            VehicleTypeEnum.TwoWheeler, "Daily ride", "BA 12 PA 3456", "Honda", "Dio", "Pearl white",
            VehicleCategoryEnum.CarJeepVan));

        Assert.False(result.IsValid);
    }
}

using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

/// <summary>
/// The walk-in domain shape: a real reservation with no rider account behind
/// it. These pin the two things that make it safe — the owner columns are empty
/// and the rider's details are only ever snapshots — plus the fact that a
/// walk-in overstays and prices exactly like an app booking.
/// </summary>
public class WalkInBookingTests
{
    private static readonly DateTimeOffset Start = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Staff = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Facility = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Booking WalkIn(
        int hours = 2,
        decimal pricePerHour = 40m,
        decimal platformFee = 5m,
        string plate = "BA 3 JA 1234")
        => Booking.CreateWalkIn(
            Facility,
            VehicleTypeEnum.TwoWheeler,
            Start,
            Start.AddHours(hours),
            pricePerHour,
            hours,
            platformFee,
            10,
            "Ram Bahadur",
            "9800000000",
            plate,
            Staff);

    [Fact]
    public void A_Walk_In_Has_No_Account_And_Keeps_The_Rider_Details_As_Snapshots()
    {
        var booking = WalkIn();

        Assert.Null(booking.UserId);
        Assert.Null(booking.VehicleId);
        Assert.True(booking.IsWalkInBooking);
        Assert.Equal("Ram Bahadur", booking.WalkInRiderName);
        Assert.Equal("9800000000", booking.WalkInPhoneNumber);
        Assert.Equal("BA 3 JA 1234", booking.WalkInPlateNumber);
        // The staff member who opened it is recorded: no account, but not anonymous.
        Assert.Equal(Staff, booking.OpenedByStaffId);
    }

    [Fact]
    public void A_Walk_In_Opens_As_An_Unpaid_Hold()
    {
        var booking = WalkIn();

        Assert.Equal(BookingStatusEnum.PendingPayment, booking.Status);
        Assert.True(booking.HoldExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.NotEqual(Guid.Empty, booking.PassNonce);
    }

    [Fact]
    public void A_Walk_In_Keeps_The_Plate_Captured_At_The_Gate()
    {
        // Plate is required at creation — it is what staff match the physical
        // vehicle against when the rider's QR will not scan.
        Assert.Equal("BA 3 JA 1234", WalkIn(plate: "BA 3 JA 1234").WalkInPlateNumber);
        Assert.Equal("BA 3 JA 1234", WalkIn(plate: "  BA 3 JA 1234  ").WalkInPlateNumber);
    }

    [Fact]
    public void A_Walk_In_Prices_Off_The_Snapshot_Rate()
    {
        var booking = WalkIn(hours: 3, pricePerHour: 40m, platformFee: 5m);

        Assert.Equal(120m, booking.TotalAmountNpr);
        Assert.Equal(0m, booking.PlatformFeeNpr);
        Assert.Equal(120m, booking.ProviderAmountNpr);
        Assert.Equal(3, booking.BillableHours);
    }

    [Fact]
    public void A_Normal_Booking_Is_Not_A_Walk_In_And_Has_No_Snapshots()
    {
        var booking = Booking.Create(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Facility,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            VehicleTypeEnum.FourWheeler,
            Start,
            Start.AddHours(2),
            100m,
            2,
            5m,
            10);

        Assert.False(booking.IsWalkInBooking);
        Assert.Null(booking.WalkInRiderName);
        Assert.Null(booking.WalkInPhoneNumber);
        Assert.Null(booking.WalkInPlateNumber);
    }

    [Fact]
    public void A_Walk_In_Parks_And_Exits_Like_Any_Other_Booking()
    {
        var booking = WalkIn();
        booking.Confirm();
        booking.Activate(Start, Staff);

        booking.Complete(Start.AddMinutes(45), Staff);

        Assert.Equal(BookingStatusEnum.Completed, booking.Status);
        Assert.Equal(45, booking.ActualStayMinutes);
        Assert.Equal(0, booking.OverstayMinutes);
        Assert.Equal(Staff, booking.EnteredByStaffId);
        Assert.Equal(Staff, booking.ExitedByStaffId);
    }

    [Fact]
    public void A_Walk_In_Overstays_On_The_Same_Measurement_As_An_App_Booking()
    {
        // Prepaid two hours, out after three hours and fifteen minutes: the same
        // 75-minute overstay an app booking would record, so the rider is billed
        // the same way once the charge is snapshotted.
        var booking = WalkIn(hours: 2);
        booking.Confirm();
        booking.Activate(Start, Staff);

        booking.Complete(Start.AddHours(3).AddMinutes(15), Staff);

        Assert.Equal(195, booking.ActualStayMinutes);
        Assert.Equal(75, booking.OverstayMinutes);
    }

    [Fact]
    public void Acknowledging_The_Payment_Is_The_Entry_And_Starts_The_Paid_Clock()
    {
        // The rider is at the barrier with the staff member who took the money,
        // so there is no second scan: the acknowledgement admits the vehicle.
        var booking = WalkIn(hours: 2);
        booking.Confirm();

        var admitted = Start.AddMinutes(7);
        booking.ActivateWalkIn(admitted, Staff);

        Assert.Equal(BookingStatusEnum.Active, booking.Status);
        Assert.Equal(admitted, booking.EnteredAtUtc);
        Assert.Equal(Staff, booking.EnteredByStaffId);

        // Two hours bought means two hours of parking, not two hours counted
        // from when the desk started typing.
        Assert.Equal(admitted.AddHours(2), booking.EndsAtUtc);
    }

    [Fact]
    public void A_Walk_In_Admitted_By_Acknowledgement_Exits_Without_An_Entry_Scan()
    {
        var booking = WalkIn(hours: 2);
        booking.Confirm();

        var admitted = Start.AddMinutes(7);
        booking.ActivateWalkIn(admitted, Staff);

        // The photographed QR is only ever used here, on the way out.
        booking.Complete(admitted.AddMinutes(90), Staff);

        Assert.Equal(BookingStatusEnum.Completed, booking.Status);
        Assert.Equal(90, booking.ActualStayMinutes);
        Assert.Equal(0, booking.OverstayMinutes);
    }

    [Fact]
    public void A_Walk_In_Overstay_Is_Measured_From_The_Acknowledged_Entry()
    {
        var booking = WalkIn(hours: 2);
        booking.Confirm();

        var admitted = Start.AddMinutes(7);
        booking.ActivateWalkIn(admitted, Staff);

        booking.Complete(admitted.AddHours(2).AddMinutes(20), Staff);

        Assert.Equal(20, booking.OverstayMinutes);
        Assert.Equal(admitted.AddHours(2).AddMinutes(20), booking.ExitedAtUtc);
    }

    [Fact]
    public void A_Walk_In_Payment_Is_Not_Tied_To_A_Rider_Account()
    {
        var payment = Payment.Create(Guid.NewGuid(), null, PaymentGatewayEnum.Khalti, 8_000);

        Assert.Null(payment.UserId);
        Assert.Equal(PaymentStatusEnum.Initiated, payment.Status);
        Assert.Null(payment.VerifiedManuallyAtUtc);
    }

    [Fact]
    public void Manual_Verification_Settles_The_Payment_And_Records_Who_Confirmed_It()
    {
        // The "rider paid, webhook is slow" override: money is treated as
        // settled on a staff member's word, but the audit trail is explicit and
        // no gateway transaction id is invented.
        var payment = Payment.Create(Guid.NewGuid(), null, PaymentGatewayEnum.Khalti, 8_000);

        payment.MarkManuallyVerified(Staff);

        Assert.Equal(PaymentStatusEnum.Completed, payment.Status);
        Assert.True(payment.IsSettled);
        Assert.Equal(Staff, payment.VerifiedManuallyByStaffId);
        Assert.NotNull(payment.VerifiedManuallyAtUtc);
        Assert.NotNull(payment.PaidAtUtc);
        Assert.Null(payment.GatewayTransactionId);
    }

    [Fact]
    public void A_Walk_In_Overstay_Payment_Is_Not_Tied_To_A_Rider_Account()
    {
        var payment = OverstayPayment.Create(Guid.NewGuid(), null, PaymentGatewayEnum.Khalti, 4_000);

        Assert.Null(payment.UserId);
        Assert.Equal(PaymentStatusEnum.Initiated, payment.Status);
    }

    [Fact]
    public void Manual_Verification_Settles_A_Walk_In_Overstay()
    {
        var payment = OverstayPayment.Create(Guid.NewGuid(), null, PaymentGatewayEnum.Khalti, 4_000);

        payment.MarkManuallyVerified(Staff);

        Assert.Equal(PaymentStatusEnum.Completed, payment.Status);
        Assert.True(payment.IsSettled);
        Assert.Equal(Staff, payment.VerifiedManuallyByStaffId);
        Assert.NotNull(payment.VerifiedManuallyAtUtc);
        Assert.Null(payment.GatewayTransactionId);
    }
}

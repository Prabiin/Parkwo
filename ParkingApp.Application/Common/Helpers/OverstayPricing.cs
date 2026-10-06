namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Overstay pricing. The grace window is consumed first and only the minutes
/// beyond it are charged, rounded up to whole hours — the same convention as
/// <see cref="BookingPricing.BillableHours"/>, so an overstay hour costs what a
/// prepaid hour costs and staff can explain the number at the barrier.
/// </summary>
public static class OverstayPricing
{
    /// <param name="BillableHours">Whole hours charged, always at least 1.</param>
    /// <param name="AmountPaisa">The paisa integer gateways charge in.</param>
    public readonly record struct Charge(int BillableHours, long AmountPaisa);

    /// <summary>
    /// What an overstay costs, or null when nothing is owed: no overstay, an
    /// overstay inside the grace window, or a facility that prices the stay at
    /// zero. Callers should treat "a charge exists" as the overstay flag rather
    /// than the raw minute count, so a free facility never shows a payment
    /// button that could only ever fail.
    /// </summary>
    public static Charge? Calculate(decimal pricePerHourNpr, int overstayMinutes, int graceMinutes)
    {
        if (overstayMinutes <= 0)
            return null;

        var billableMinutes = overstayMinutes - Math.Max(0, graceMinutes);
        if (billableMinutes <= 0)
            return null;

        var billableHours = (int)Math.Ceiling(billableMinutes / 60d);
        var amountPaisa = BookingPricing.ToPaisa(pricePerHourNpr * billableHours);

        return amountPaisa <= 0
            ? null
            : new Charge(billableHours, amountPaisa);
    }

    /// <summary>
    /// The one sentence the exit scan returns to staff and the exit summary
    /// returns to the rider, built here so the two can never contradict each
    /// other. A charge always wins the wording, because it is the branch that
    /// asks for money.
    /// </summary>
    public static string DescribeExitMessage(int overstayMinutes, int graceMinutes, Charge? charge)
    {
        if (charge is not null)
            return "Overstay detected. Please proceed to payment.";

        return StayCalculator.IsOverstay(overstayMinutes, graceMinutes)
            ? $"Exit recorded. Vehicle overstayed by {overstayMinutes} minutes."
            : "Exit recorded. Space released.";
    }
}

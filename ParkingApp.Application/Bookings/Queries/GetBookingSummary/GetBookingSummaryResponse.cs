namespace ParkingApp.Application.Bookings.Queries.GetBookingSummary;

/// <summary>
/// The confirmation screen shown after a booking is created but before its
/// prepaid payment starts. Everything is in rupees and local time so the app
/// can render it directly; the amount is the total the payment will actually
/// charge, not a client-supplied number.
/// <para>
/// Deliberately no fee breakdown. The Parkwo rate is what the rider pays and it
/// already covers the payment gateway's cut; how that money is later split
/// between Parkwo and the facility owner is settlement, not something the rider
/// is being charged for.
/// </para>
/// </summary>
public record GetBookingSummaryResponse(
    Guid BookingId,
    string FacilityName,
    string VehicleName,
    string VehicleNumber,
    string VehicleTypeDescription,
    string StartsAtLocal,
    string EndsAtLocal,
    decimal PricePerHourNpr,
    int BillableHours,
    decimal TotalAmountNpr,
    string Currency,
    string HoldExpiresAtLocal);
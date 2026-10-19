using ParkingApp.Application.Vehicles.Queries.GetVehicles;

namespace ParkingApp.Application.Bookings.Queries.GetBookingInit;

/// <summary>
/// Everything the "Book a spot" screen needs before a slot is chosen: the
/// parking's name, the spaces it has per vehicle type, and the rider's
/// registered vehicles. The vehicle list is what the rider picks from — the
/// chosen <c>VehicleId</c> is what <c>CreateBookingCommand</c> takes, so the
/// server (not the client) resolves the vehicle type and applies the right
/// charge.
/// </summary>
/// <remarks>
/// <see cref="Vehicles"/> is filtered to the types this lot actually serves, so
/// a rider is never offered a category the parking cannot take — the picker
/// stays honest without the client greying anything out. The occupancies are
/// included so the screen can explain an empty list when the rider owns no
/// suitable vehicle. Availability is not here: it depends on the picked vehicle
/// type and day, and changes as the rider browses (see GetFacilityAvailability).
/// </remarks>
public record GetBookingInitResponse(
    Guid FacilityId,
    string FacilityName,
    int TwoWheelerOccupancy,
    int FourWheelerOccupancy,
    IReadOnlyList<VehicleItemResponse> Vehicles,
    bool HasVehicle);

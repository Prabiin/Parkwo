using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Queries.GetFacilityAvailability;

/// <summary>One hour of the booking picker, with the count behind its colour.</summary>
public record AvailabilitySlotResponse(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    string FromLocal,
    string ToLocal,
    int Available,
    bool IsBooked);

/// <summary>
/// The hourly availability grid a rider browses after tapping a pin. Asked
/// either as a whole local day (<see cref="Date"/>, the picker's default) or as
/// one exact window (<see cref="Date"/> null) — the latter for checking a
/// multi-slot selection before committing it.
/// </summary>
public record GetFacilityAvailabilityResponse(
    Guid FacilityId,
    string FacilityName,
    DateOnly? Date,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    int Capacity,
    // Each slot is advisory: two riders can still race for the last space, so
    // the booking gate (CreateBookingCommand) remains the authority.
    IReadOnlyList<AvailabilitySlotResponse> Slots);

namespace ParkingApp.Application.Facilities.Commands.CreateSpots;

public record CreateParkingSpotsResponse(
    Guid FacilityId,
    IReadOnlyList<ParkingSpotItemResponse> Spots,
    int TwoWheelerCount,
    int FourWheelerCount);
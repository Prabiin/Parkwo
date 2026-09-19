using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities;

public record ParkingSpotItemResponse(
    Guid Id,
    string SpotNumber,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    decimal PricePerHourNpr,
    bool IsActive);
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Commands.Create;

public record CreateVehicleResponse(
    Guid Id,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    string Name,
    string VehicleNumber);
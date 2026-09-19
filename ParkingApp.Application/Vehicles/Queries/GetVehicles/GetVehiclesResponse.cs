using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Queries.GetVehicles;

public record VehicleItemResponse(
    Guid Id,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    string Name,
    string VehicleNumber);

public record GetVehiclesResponse(
    IReadOnlyList<VehicleItemResponse> Vehicles,
    bool HasVehicle);
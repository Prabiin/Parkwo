using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Queries.GetVehicles;

public record VehicleItemResponse(
    Guid Id,
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription,
    VehicleCategoryEnum VehicleCategory,
    string VehicleCategoryDescription,
    string Name,
    string VehicleNumber,
    string Brand,
    string Model,
    string Color);

public record GetVehiclesResponse(
    IReadOnlyList<VehicleItemResponse> Vehicles,
    bool HasVehicle);
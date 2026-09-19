using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetRiders;

public record VehicleTypeItemResponse(
    VehicleTypeEnum VehicleType,
    string VehicleTypeDescription);

public record RiderItemResponse(
    Guid Id,
    string? FullName,
    string PhoneNumber,
    string? Email,
    bool IsPhoneVerified,
    bool IsProfileComplete,
    DateTimeOffset MemberSince,
    int VehiclesCount,
    IReadOnlyList<VehicleTypeItemResponse> VehicleTypes);

public record GetRidersResponse(
    IReadOnlyList<RiderItemResponse> Riders);
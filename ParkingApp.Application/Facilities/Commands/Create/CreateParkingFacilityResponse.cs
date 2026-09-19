using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.Create;

public record CreateParkingFacilityResponse(
    Guid Id,
    Guid ProviderId,
    string Name,
    string? Description,
    string Address,
    double? Latitude,
    double? Longitude,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    int TwoWheelerCount,
    int FourWheelerCount);
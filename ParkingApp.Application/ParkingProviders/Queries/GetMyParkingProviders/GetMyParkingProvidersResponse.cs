using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Queries.GetMyParkingProviders;

public record MyParkingProviderItemResponse(
    Guid Id,
    ProviderTypeEnum ProviderType,
    string ProviderTypeDescription,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    Guid? OwnerUserId,
    Guid? OwnerOrganizationId);

public record GetMyParkingProvidersResponse(
    IReadOnlyList<MyParkingProviderItemResponse> ParkingProviders);
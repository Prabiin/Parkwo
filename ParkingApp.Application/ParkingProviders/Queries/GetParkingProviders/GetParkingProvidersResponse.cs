using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Queries.GetParkingProviders;

public record ParkingProviderItemResponse(
    Guid Id,
    ProviderTypeEnum ProviderType,
    string ProviderTypeDescription,
    Guid? OwnerUserId,
    Guid? OwnerOrganizationId);

public record GetParkingProvidersResponse(
    IReadOnlyList<ParkingProviderItemResponse> ParkingProviders);
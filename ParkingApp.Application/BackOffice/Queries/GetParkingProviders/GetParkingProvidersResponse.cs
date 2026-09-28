using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetParkingProviders;

public record ParkingProviderItemResponse(
    Guid Id,
    ProviderTypeEnum ProviderType,
    string ProviderTypeDescription,
    DateTimeOffset CreatedAtUtc,
    Guid? OwnerUserId,
    Guid? OwnerOrganizationId,
    string? OwnerName,
    string? OwnerContactNumber,
    string? OwnerEmail);

public record GetParkingProvidersResponse(
    IReadOnlyList<ParkingProviderItemResponse> ParkingProviders);
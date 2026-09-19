using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Commands.Create;

public record CreateParkingProviderResponse(
    Guid Id,
    ProviderTypeEnum ProviderType,
    string ProviderTypeDescription,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    Guid? OwnerUserId,
    Guid? OwnerOrganizationId);
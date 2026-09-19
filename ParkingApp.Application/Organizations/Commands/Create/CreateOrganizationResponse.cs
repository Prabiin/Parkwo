using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Organizations.Commands.Create;

public record CreateOrganizationResponse(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string ContactNumber,
    string Address,
    Guid OwnerUserId,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    OrganizationRoleEnum MyRole,
    string MyRoleDescription);
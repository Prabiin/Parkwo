using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Organizations.Queries.GetOrganizations;

public record OrganizationItemResponse(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string ContactNumber,
    string Address,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    OrganizationRoleEnum Role,
    string RoleDescription,
    string? RejectionReason);

public record GetOrganizationsResponse(
    IReadOnlyList<OrganizationItemResponse> Organizations);
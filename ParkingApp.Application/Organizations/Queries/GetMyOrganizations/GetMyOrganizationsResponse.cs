using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Organizations.Queries.GetMyOrganizations;

public record MyOrganizationItemResponse(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string ContactNumber,
    string Address,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    OrganizationRoleEnum MyRole,
    string MyRoleDescription);

public record GetMyOrganizationsResponse(
    IReadOnlyList<MyOrganizationItemResponse> Organizations);
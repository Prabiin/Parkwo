using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetOrganizations;

public record OrganizationItemResponse(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string ContactNumber,
    string Address,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    DateTimeOffset CreatedAtUtc,
    Guid OwnerUserId,
    string? OwnerName,
    string? OwnerPhoneNumber,
    string? RejectionReason);

public record GetOrganizationsResponse(
    IReadOnlyList<OrganizationItemResponse> Organizations);
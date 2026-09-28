using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetLicenses;

public record DrivingLicenseItemResponse(
    Guid Id,
    Guid UserId,
    string? UserFullName,
    string? UserPhoneNumber,
    string LicenseNumber,
    IReadOnlyList<LicenseCategoryEnum> Categories,
    IReadOnlyList<string> CategoryDescriptions,
    string FrontImageUrl,
    string BackImageUrl,
    DateOnly ExpiryDate,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc);

public record GetLicensesResponse(
    IReadOnlyList<DrivingLicenseItemResponse> Licenses);

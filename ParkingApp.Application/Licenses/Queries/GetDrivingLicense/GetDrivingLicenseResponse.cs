using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Licenses.Queries.GetDrivingLicense;

public record DrivingLicenseResponse(
    Guid Id,
    string LicenseNumber,
    IReadOnlyList<LicenseCategoryEnum> Categories,
    IReadOnlyList<string> CategoryDescriptions,
    string FrontImageUrl,
    string BackImageUrl,
    DateOnly ExpiryDate,
    ApprovalStatusEnum ApprovalStatus,
    string ApprovalStatusDescription,
    string? RejectionReason);

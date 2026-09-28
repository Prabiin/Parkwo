using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Domain;

public class DrivingLicense : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string LicenseNumber { get; private set; } = default!;
    public List<LicenseCategoryEnum> Categories { get; private set; } = [];
    public string FrontImageUrl { get; private set; } = default!;
    public string BackImageUrl { get; private set; } = default!;
    public DateOnly ExpiryDate { get; private set; }
    public ApprovalStatusEnum ApprovalStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    private DrivingLicense() { }

    public static DrivingLicense Create(
        Guid userId,
        string licenseNumber,
        IEnumerable<LicenseCategoryEnum> categories,
        string frontImageUrl,
        string backImageUrl,
        DateOnly expiryDate)
    {
        return new DrivingLicense
        {
            UserId = userId,
            LicenseNumber = licenseNumber.Trim().ToUpperInvariant(),
            Categories = categories.Distinct().ToList(),
            FrontImageUrl = frontImageUrl.Trim(),
            BackImageUrl = backImageUrl.Trim(),
            ExpiryDate = expiryDate,
            ApprovalStatus = ApprovalStatusEnum.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// BackOffice compliance decision. Clearing the reason on verify;
    /// a later resubmission resets to Pending via Resubmit.
    /// </summary>
    public void ApplyReview(ApprovalStatusEnum status, string? rejectionReason)
    {
        ApprovalStatus = status;
        RejectionReason = status == ApprovalStatusEnum.Verified
            ? null
            : string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Re-submits after a rejection: replaces the card details and
    /// sends the row back to Pending for compliance review.
    /// </summary>
    public void Resubmit(
        string licenseNumber,
        IEnumerable<LicenseCategoryEnum> categories,
        string frontImageUrl,
        string backImageUrl,
        DateOnly expiryDate)
    {
        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
        Categories = categories.Distinct().ToList();
        FrontImageUrl = frontImageUrl.Trim();
        BackImageUrl = backImageUrl.Trim();
        ExpiryDate = expiryDate;
        ApprovalStatus = ApprovalStatusEnum.Pending;
        RejectionReason = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}

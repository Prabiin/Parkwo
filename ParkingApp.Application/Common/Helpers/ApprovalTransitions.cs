using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

/// <summary>
/// Legal approval transitions for every approvable row
/// (organizations, providers, facilities, driving licenses).
/// Fresh submissions start Pending; Verified rows can only be
/// sent back to UnderReview (re-audit), never silently reverted;
/// Rejected rows re-enter only via UnderReview.
/// </summary>
public static class ApprovalTransitions
{
    public static bool IsAllowed(ApprovalStatusEnum from, ApprovalStatusEnum to)
    {
        if (from == to)
            return false;

        return from switch
        {
            ApprovalStatusEnum.Pending =>
                to is ApprovalStatusEnum.Verified
                    or ApprovalStatusEnum.UnderReview
                    or ApprovalStatusEnum.Rejected,
            ApprovalStatusEnum.UnderReview =>
                to is ApprovalStatusEnum.Verified
                    or ApprovalStatusEnum.Rejected,
            ApprovalStatusEnum.Rejected =>
                to is ApprovalStatusEnum.UnderReview,
            ApprovalStatusEnum.Verified =>
                to is ApprovalStatusEnum.UnderReview,
            _ => false
        };
    }
}

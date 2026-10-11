using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Application.Common.Helpers;

public static class GateAccess
{
    /// <summary>
    /// True when the caller may operate the gate at this facility. The gate is
    /// staffed by Parkwo field staff, so the trust boundary is the staff record
    /// itself: the member must be live (not deactivated or soft-deleted) and
    /// assigned to this facility. Provider owners and organization members have
    /// no scan rights at all.
    /// </summary>
    public static Task<bool> CanScanAsync(
        IApplicationDbContext context,
        Guid staffId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
        => context.Staffs
            .AnyAsync(s => s.Id == staffId
                           && !s.IsDeleted
                           && s.IsActive
                           && s.FacilityId == facilityId,
                cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

public static class GateAccess
{
    /// <summary>
    /// True when the caller may scan passes at this facility. Gate staff are the
    /// provider that owns the lot, or a Monitor/Admin/Owner in that provider's
    /// organization — the same trust boundary the rest of the provider APIs use.
    /// </summary>
    public static Task<bool> CanScanAsync(
        IApplicationDbContext context,
        Guid userId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
        => context.ParkingFacilities
            .Where(f => f.Id == facilityId)
            .AnyAsync(f => context.ParkingProviders
                .Any(p => p.Id == f.ProviderId
                          && (p.OwnerUserId == userId
                              || (p.OwnerOrganizationId != null
                                  && context.UserOrganizations.Any(m =>
                                      m.OrganizationId == p.OwnerOrganizationId
                                      && m.UserId == userId
                                      && (m.Role == OrganizationRoleEnum.Owner
                                          || m.Role == OrganizationRoleEnum.Admin
                                          || m.Role == OrganizationRoleEnum.Monitor))))),
                cancellationToken);
}

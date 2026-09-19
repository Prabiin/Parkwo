using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Common.Helpers;

public static class ProviderOwnership
{
    /// <summary>
    /// True when the caller owns the provider: it is their individual profile, or it belongs to an
    /// organization where they hold the Owner role.
    /// </summary>
    public static Task<bool> IsOwnerAsync(
        IApplicationDbContext context,
        Guid userId,
        Guid providerId,
        CancellationToken cancellationToken = default)
        => context.ParkingProviders
            .Where(p => p.Id == providerId)
            .AnyAsync(p => p.OwnerUserId == userId
                           || (p.OwnerOrganizationId != null
                               && context.UserOrganizations.Any(m =>
                                   m.OrganizationId == p.OwnerOrganizationId
                                   && m.UserId == userId
                                   && m.Role == OrganizationRoleEnum.Owner)),
                cancellationToken);
}
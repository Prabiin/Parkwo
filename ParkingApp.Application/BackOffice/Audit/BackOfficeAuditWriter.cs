using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Audit;

namespace ParkingApp.Application.BackOffice.Audit;

/// <inheritdoc />
public sealed class BackOfficeAuditWriter(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IBackOfficeAuditWriter
{
    public async Task RecordAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = currentUser.UserId;

        // The name is copied rather than joined: the actor may be deleted
        // later, and the trail still has to say who did it.
        string? actorUserName = null;

        if (actorUserId is not null)
        {
            actorUserName = await context.BackOfficeUsers
                .Where(u => u.Id == actorUserId.Value)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        context.BackOfficeAuditLogs.Add(new BackOfficeAuditLog
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            IpAddress = currentUser.IpAddress,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
    }
}

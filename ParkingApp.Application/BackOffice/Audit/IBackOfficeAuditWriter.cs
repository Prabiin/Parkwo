namespace ParkingApp.Application.BackOffice.Audit;

/// <summary>
/// Records what a BackOffice operator did.
/// <para>
/// The row is added to the unit of work rather than saved on its own, so the
/// audit entry and the change it describes are written together — an action
/// without its trail, or a trail for an action that rolled back, would both be
/// misleading.
/// </para>
/// </summary>
public interface IBackOfficeAuditWriter
{
    /// <summary>
    /// Queues an audit entry. It is persisted by the caller's own
    /// <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="action">What was done, e.g. "Create", "Update", "Delete".</param>
    /// <param name="entityType">What it was done to, e.g. "BackOfficeUser".</param>
    /// <param name="entityId">The id of the affected record, when there is one.</param>
    /// <param name="details">Readable detail about the change, when it is worth keeping.</param>
    Task RecordAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}

namespace ParkingApp.Domain.BackOffice.Audit;

/// <summary>
/// One consequential BackOffice action, recorded so it can be answered later:
/// who did it, to what, and when.
/// <para>
/// The console can grant access and change what the business is allowed to do,
/// so the questions "who gave this role Edit on Users" and "who reset this
/// admin's password" have to be answerable. The actor's name is copied onto
/// the row rather than joined, because the actor may be deleted afterwards
/// and the record must still say who it was.
/// </para>
/// <para>
/// Append-only: there is no update or delete path for these rows.
/// </para>
/// </summary>
public class BackOfficeAuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>When the action happened.</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>The BackOffice user who performed the action.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// The actor's username, copied at the time of the action so the record
    /// survives the actor being deleted.
    /// </summary>
    public string? ActorUserName { get; set; }

    /// <summary>Where the request came from.</summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// What was done, e.g. "Create", "Update", "Delete", "ResendInvite",
    /// "ChangePassword".
    /// </summary>
    public string Action { get; set; } = default!;

    /// <summary>What it was done to, e.g. "BackOfficeUser", "BackOfficeRole".</summary>
    public string EntityType { get; set; } = default!;

    /// <summary>The id of the affected record.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Free-form detail about the change — the permissions a role gained or
    /// lost, for example. Readable by a human, not a machine format.
    /// </summary>
    public string? Details { get; set; }
}

using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain.BackOffice.Role;

/// <summary>
/// BackOffice role (e.g., SuperAdmin, Admin, Viewer).
/// <para>
/// A role has two independent ways of being switched off, and they are not
/// the same thing:
/// <list type="bullet">
/// <item>Inactive — a pause. The role still exists, still shows in the list,
/// still carries its menu actions, and can be switched back on by an
/// administrator at any time.</item>
/// <item>Deleted — gone from the console. The row and its menu actions are
/// kept so the history of who had what survives, but the role cannot be
/// brought back through the portal.</item>
/// </list>
/// Neither one touches the role's permissions: switching a role off is about
/// the role, not about what it can do. Restoring it must not require
/// re-granting access.
/// </para>
/// </summary>
public class BackOfficeRole : AuditableEntity
{
    /// <summary>
    /// Unique role name (e.g., "SuperAdmin", "Admin", "Viewer"). Unique among
    /// roles that have not been deleted.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-defined role (cannot be deleted/modified by users).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// False pauses the role: it stays in the list and can be switched back on,
    /// but nobody holding it can sign in.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// True once the role has been deleted. A deleted role is hidden from the
    /// console entirely and cannot be restored from it.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Users assigned to this role.
    /// </summary>
    public ICollection<BackOfficeUserRole> UserRoles { get; set; } = new List<BackOfficeUserRole>();

    /// <summary>
    /// Menu actions assigned to this role. Kept when the role is deactivated
    /// or deleted, so switching a role off never silently strips access that
    /// would then have to be granted again.
    /// </summary>
    public ICollection<BackOfficeRoleMenuAction> RoleMenuActions { get; set; } = new List<BackOfficeRoleMenuAction>();

    /// <summary>
    /// Pauses or resumes the role. The menu actions are untouched.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deletes the role without removing the row or its permissions. A deleted
    /// role is inactive too, so nobody holding it can sign in.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}

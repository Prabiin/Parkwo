using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain.BackOffice.Role;

/// <summary>
/// BackOffice role (e.g., SuperAdmin, Admin, Viewer).
/// </summary>
public class BackOfficeRole : AuditableEntity
{
    /// <summary>
    /// Unique role name (e.g., "SuperAdmin", "Admin", "Viewer").
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
    /// Users assigned to this role.
    /// </summary>
    public ICollection<BackOfficeUserRole> UserRoles { get; set; } = new List<BackOfficeUserRole>();

    /// <summary>
    /// Menu actions assigned to this role.
    /// </summary>
    public ICollection<BackOfficeRoleMenuAction> RoleMenuActions { get; set; } = new List<BackOfficeRoleMenuAction>();
}
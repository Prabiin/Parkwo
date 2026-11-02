using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Domain.BackOffice.Menu;

/// <summary>
/// Action/permission available for a BackOffice menu.
/// </summary>
public class BackOfficeMenuAction : AuditableEntity
{
    /// <summary>
    /// The menu key this action belongs to (e.g., "Role", "User", "Dashboard").
    /// </summary>
    public string MenuKey { get; set; } = default!;

    /// <summary>
    /// Action key (e.g., "List", "Add", "Edit", "Delete", "View", "Unlock").
    /// </summary>
    public string ActionKey { get; set; } = default!;

    /// <summary>
    /// Display name for the action.
    /// </summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>
    /// Whether this action is active/enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation to the menu.
    /// </summary>
    public BackOfficeMenu? Menu { get; set; }

    /// <summary>
    /// Roles that have this action.
    /// </summary>
    public ICollection<BackOfficeRoleMenuAction> RoleMenuActions { get; set; } = new List<BackOfficeRoleMenuAction>();

    /// <summary>
    /// Composite key: MenuKey + ActionKey (e.g., "Role:List", "User:Add").
    /// </summary>
    public string CompositeKey => $"{MenuKey}:{ActionKey}";
}
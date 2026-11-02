using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain.BackOffice.Menu;

/// <summary>
/// BackOffice menu item (hierarchical navigation).
/// </summary>
public class BackOfficeMenu : AuditableEntity
{
    /// <summary>
    /// Unique key for the menu (e.g., "Dashboard", "Role", "User").
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Display title (e.g., "Dashboard", "Role Management").
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Frontend route path (e.g., "/dashboard", "/access-control/role").
    /// Null for parent/group menus without a direct route.
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Icon identifier (e.g., "dashboard", "badge", "group").
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Sort order within parent.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Parent menu ID (null for top-level menus).
    /// </summary>
    public Guid? ParentMenuId { get; set; }

    /// <summary>
    /// Navigation to parent menu.
    /// </summary>
    public BackOfficeMenu? ParentMenu { get; set; }

    /// <summary>
    /// Child menus.
    /// </summary>
    public ICollection<BackOfficeMenu> Children { get; set; } = new List<BackOfficeMenu>();

    /// <summary>
    /// Actions available for this menu.
    /// </summary>
    public ICollection<BackOfficeMenuAction> Actions { get; set; } = new List<BackOfficeMenuAction>();
}
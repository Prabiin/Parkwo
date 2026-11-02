using ParkingApp.Domain.Common.Base;
using ParkingApp.Domain.BackOffice.Menu;

namespace ParkingApp.Domain.BackOffice.Role;

/// <summary>
/// Many-to-many relationship between BackOfficeRole and BackOfficeMenuAction.
/// </summary>
public class BackOfficeRoleMenuAction : AuditableEntity
{
    /// <summary>
    /// The BackOffice role ID.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The BackOffice menu action ID.
    /// </summary>
    public Guid MenuActionId { get; set; }

    /// <summary>
    /// Navigation to the role.
    /// </summary>
    public BackOfficeRole? Role { get; set; }

    /// <summary>
    /// Navigation to the menu action.
    /// </summary>
    public BackOfficeMenuAction? MenuAction { get; set; }
}
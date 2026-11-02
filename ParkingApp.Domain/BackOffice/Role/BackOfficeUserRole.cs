using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain.BackOffice.Role;

/// <summary>
/// Many-to-many relationship between BackOfficeUser and BackOfficeRole.
/// </summary>
public class BackOfficeUserRole : AuditableEntity
{
    /// <summary>
    /// The BackOffice user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The BackOffice role ID.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation to the user.
    /// </summary>
    public BackOfficeUser? User { get; set; }

    /// <summary>
    /// Navigation to the role.
    /// </summary>
    public BackOfficeRole? Role { get; set; }
}
using ParkingApp.Domain.Common.Base;

namespace ParkingApp.Domain;

public class BackOfficeUser : AuditableEntity
{
    public string UserName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
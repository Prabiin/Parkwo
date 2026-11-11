namespace ParkingApp.Application.BackOffice.Queries.GetRoleById;

public record GetRoleByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    /// <summary>False means the role is paused; it can be switched back on.</summary>
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<RoleMenuOption> Menus,
    int UserCount);

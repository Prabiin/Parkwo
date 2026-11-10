namespace ParkingApp.Application.BackOffice.Queries.GetRoleById;

public record GetRoleByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<RoleMenuOption> Menus,
    int UserCount);

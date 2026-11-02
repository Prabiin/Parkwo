namespace ParkingApp.Application.BackOffice.Queries.GetRoleById;

public record RolePermissionResponse(
    string MenuKey,
    string ActionKey,
    string DisplayName);

public record GetRoleByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<RolePermissionResponse> Permissions,
    int UserCount);
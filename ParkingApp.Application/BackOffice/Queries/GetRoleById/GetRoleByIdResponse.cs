namespace ParkingApp.Application.BackOffice.Queries.GetRoleById;

public record RolePermissionResponse(
    Guid Id,
    string MenuKey,
    string ActionKey,
    string DisplayName,
    bool IsAssigned);

public record GetRoleByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<RolePermissionResponse> Permissions,
    int UserCount);
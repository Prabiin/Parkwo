namespace ParkingApp.Application.BackOffice.Queries.GetRoles;

public record RoleItemResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    /// <summary>
    /// False means the role is paused: still listed, still carries its
    /// permissions, but nobody holding it can sign in until it is switched on.
    /// </summary>
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public record GetRolesResponse(
    IReadOnlyList<RoleItemResponse> Roles,
    int TotalCount,
    int Page,
    int PageSize);
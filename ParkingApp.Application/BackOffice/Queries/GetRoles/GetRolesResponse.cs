namespace ParkingApp.Application.BackOffice.Queries.GetRoles;

public record RoleItemResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public record GetRolesResponse(
    IReadOnlyList<RoleItemResponse> Roles,
    int TotalCount,
    int Page,
    int PageSize);
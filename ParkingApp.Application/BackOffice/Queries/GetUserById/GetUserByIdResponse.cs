namespace ParkingApp.Application.BackOffice.Queries.GetUserById;

public record UserRoleResponse(
    Guid Id,
    string Name,
    bool IsSystem);

public record GetUserByIdResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<UserRoleResponse> Roles);
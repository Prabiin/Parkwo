namespace ParkingApp.Application.BackOffice.Queries.GetUsers;

public record UserItemResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public record GetUsersResponse(
    IReadOnlyList<UserItemResponse> Users,
    int TotalCount,
    int Page,
    int PageSize);
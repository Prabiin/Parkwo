namespace ParkingApp.Application.BackOffice.Queries.GetUsers;

public record UserItemResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    bool IsActive,
    /// <summary>
    /// True while the account is still on a Parkwo-generated password, i.e. the
    /// user has never signed in and set their own. Drives the "resend invite"
    /// action, which is only offered while this is true.
    /// </summary>
    bool MustChangePassword,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public record GetUsersResponse(
    IReadOnlyList<UserItemResponse> Users,
    int TotalCount,
    int Page,
    int PageSize);
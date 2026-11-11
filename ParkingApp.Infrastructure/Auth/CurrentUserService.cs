using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ParkingApp.Application.Common.Interfaces;

namespace ParkingApp.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId =>
        !IsStaff && TryGetSubject(out var id) ? id : null;

    public Guid? StaffId =>
        IsStaff && TryGetSubject(out var id) ? id : null;

    // The role claim is the discriminator. A rider token has no role claim, a
    // BackOffice token carries "BackOffice", and a staff token carries "Staff",
    // so exactly one of UserId/StaffId can ever be populated.
    public bool IsStaff =>
        string.Equals(
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role),
            StaffRoles.Staff,
            StringComparison.Ordinal);

    /// <summary>
    /// The caller's IP, taken from the connection. A proxy in front of the app
    /// (Render, a load balancer) makes this the proxy's address unless the
    /// forwarded headers are applied first — Program.cs does that.
    /// </summary>
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private bool TryGetSubject(out Guid id)
    {
        id = Guid.Empty;

        return _httpContextAccessor.HttpContext?.User
                   .FindFirstValue(ClaimTypes.NameIdentifier) is { } sub
               && Guid.TryParse(sub, out id);
    }
}

/// <summary>Role string carried by a staff access token. Shared by the token service and the policy.</summary>
public static class StaffRoles
{
    public const string Staff = "Staff";
}

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
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } sub
        && Guid.TryParse(sub, out var userId)
            ? userId
            : null;
}
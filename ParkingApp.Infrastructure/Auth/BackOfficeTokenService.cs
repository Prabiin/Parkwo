using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.Common.Constants;
using ParkingApp.Application.Configuration;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Auth;

public class BackOfficeTokenService : IBackOfficeTokenService
{
    private readonly JwtSettings _jwtSettings;

    public BackOfficeTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(BackOfficeUser user, IReadOnlyList<string> permissions, bool isSuperAdmin)
    {
        // Jwt:Secret must match the key middleware validates with — it signs
        // what the validation re-signs for the signature check.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "BackOffice"),
        };

        // Add permissions as claims
        foreach (var perm in permissions)
        {
            claims.Add(new Claim(BackOfficeConstants.BackOfficePermissionClaim, perm));
        }

        // Add SuperAdmin claim if applicable
        if (isSuperAdmin)
        {
            claims.Add(new Claim(BackOfficeConstants.IsSuperAdminClaim, "true"));
        }

        // Issuer, audience and lifetime all come from Jwt settings in configuration.
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
using ParkingApp.Domain;

namespace ParkingApp.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

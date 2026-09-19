using ParkingApp.Domain;

namespace ParkingApp.Application.Auth.Interfaces;

public interface IBackOfficeTokenService
{
    string GenerateAccessToken(BackOfficeUser user);
}
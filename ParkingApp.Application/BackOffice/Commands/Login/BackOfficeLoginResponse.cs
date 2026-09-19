namespace ParkingApp.Application.BackOffice.Commands.Login;

public record BackOfficeLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string FullName,
    string UserName,
    string Email);
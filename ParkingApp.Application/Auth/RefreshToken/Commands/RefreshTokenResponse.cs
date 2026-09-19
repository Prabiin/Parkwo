namespace ParkingApp.Application.Features.RefreshToken.Command;

public record RefreshTokenResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    bool IsProfileComplete);
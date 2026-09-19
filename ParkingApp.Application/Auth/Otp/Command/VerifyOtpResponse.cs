namespace ParkingApp.Application.Features.VerifyOtp.Command;

public record VerifyOtpResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    bool IsNewUser,
    bool IsProfileComplete);
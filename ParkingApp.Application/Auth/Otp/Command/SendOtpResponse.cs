namespace ParkingApp.Application.Features.SendOtp.Command;

public record SendOtpResponse(string Message, DateTimeOffset ExpiresAt, string? DevCode);
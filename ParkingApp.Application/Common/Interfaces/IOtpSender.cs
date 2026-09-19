namespace ParkingApp.Application.Auth.Interfaces;

public interface IOtpSender
{
    Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}

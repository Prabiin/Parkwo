namespace ParkingApp.Application.BackOffice.Commands.CreateUser;

public record CreateUserResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    bool MustChangePassword,
    /// <summary>
    /// False when the generated password could not be emailed. The account is
    /// still created and usable — resend issues a fresh password.
    /// </summary>
    bool PasswordEmailSent);

namespace ParkingApp.Application.BackOffice.Commands.CreateUser;

public record CreateUserResponse(
    Guid Id,
    string UserName);
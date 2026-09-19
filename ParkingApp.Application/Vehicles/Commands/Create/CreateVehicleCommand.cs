using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Commands.Create;

public sealed record CreateVehicleCommand(
    string VehicleType,
    string Name,
    string VehicleNumber)
    : IRequestResult<CreateVehicleCommand, CreateVehicleResponse>;

public sealed class CreateVehicleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateVehicleCommand, CreateVehicleResponse>
{
    public async Task<Result<CreateVehicleResponse>> Handle(CreateVehicleCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreateVehicleResponse>.Failure("Authentication required.", 401);

        if (!Enum.TryParse<VehicleTypeEnum>(request.VehicleType, ignoreCase: true, out var vehicleType))
            return Result<CreateVehicleResponse>.Failure("Invalid vehicle type.");

        var normalizedNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        var alreadyRegistered = await context.Vehicles
            .AnyAsync(v => v.UserId == userId
                           && v.VehicleNumber == normalizedNumber, cancellationToken);

        if (alreadyRegistered)
            return Result<CreateVehicleResponse>.Failure(
                "This vehicle number is already registered to your account.", 409);

        var vehicle = Vehicle.Create(
            userId.Value,
            vehicleType,
            request.Name,
            normalizedNumber);

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateVehicleResponse>.Success(
            new CreateVehicleResponse(
                vehicle.Id,
                vehicle.VehicleType,
                vehicle.VehicleType.ToDescription(),
                vehicle.Name,
                vehicle.VehicleNumber));
    }
}
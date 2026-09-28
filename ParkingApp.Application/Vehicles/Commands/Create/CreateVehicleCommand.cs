using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Commands.Create;

public sealed record CreateVehicleCommand(
    VehicleTypeEnum VehicleType,
    VehicleCategoryEnum VehicleCategory,
    string Name,
    string VehicleNumber,
    string Brand,
    string Model,
    string Color)
    : IRequestResult<CreateVehicleCommand, Guid>;

public sealed class CreateVehicleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateVehicleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateVehicleCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        var normalizedNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        var alreadyRegistered = await context.Vehicles
            .AnyAsync(v => v.UserId == userId
                           && v.VehicleNumber == normalizedNumber, cancellationToken);

        if (alreadyRegistered)
            return Result<Guid>.Failure(
                "This vehicle number is already registered to your account.", 409);

        var license = await context.DrivingLicenses
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (license is not null
            && license.ApprovalStatus == ApprovalStatusEnum.Verified
            && !LicenseCoverage.Covers(license.Categories, request.VehicleCategory))
            return Result<Guid>.Failure(
                $"Your verified license ({string.Join(", ", license.Categories)}) does not cover {request.VehicleCategory.ToDescription()}.", 409);

        var vehicle = Vehicle.Create(
            userId.Value,
            request.VehicleType,
            request.VehicleCategory,
            request.Name,
            normalizedNumber,
            request.Brand,
            request.Model,
            request.Color);

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(vehicle.Id);
    }
}
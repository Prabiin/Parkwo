using NetTopologySuite.Geometries;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;

namespace ParkingApp.Application.Facilities.Commands.Create;

public sealed record CreateParkingFacilityCommand(
    Guid ProviderId,
    string Name,
    string? Description,
    string Address,
    double? Latitude,
    double? Longitude,
    bool HasMarkedParkingLot,
    int TwoWheelerOccupancy,
    int FourWheelerOccupancy,
    decimal? LandAreaSqM,
    decimal TwoWheelerPricePerHourNpr,
    decimal FourWheelerPricePerHourNpr)
    : IRequestResult<CreateParkingFacilityCommand, Guid>;

public sealed class CreateParkingFacilityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingFacilityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateParkingFacilityCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, request.ProviderId, cancellationToken))
            return Result<Guid>.Failure(
                "You must own the parking provider to add a facility.", 403);

        var facility = new ParkingFacility
        {
            ProviderId = request.ProviderId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Location = request.Latitude.HasValue && request.Longitude.HasValue
                ? new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 }
                : null,
            HasMarkedParkingLot = request.HasMarkedParkingLot,
            TwoWheelerOccupancy = request.TwoWheelerOccupancy,
            FourWheelerOccupancy = request.FourWheelerOccupancy,
            LandAreaSqM = request.LandAreaSqM,
            TwoWheelerPricePerHourNpr = request.TwoWheelerPricePerHourNpr,
            FourWheelerPricePerHourNpr = request.FourWheelerPricePerHourNpr,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.ParkingFacilities.Add(facility);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(facility.Id);
    }
}
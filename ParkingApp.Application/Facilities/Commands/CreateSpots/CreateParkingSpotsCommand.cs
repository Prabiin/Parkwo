using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.CreateSpots;

public sealed record CreateParkingSpotRequest(
    string SpotNumber,
    string VehicleType,
    decimal PricePerHourNpr,
    bool? IsActive = true);

public sealed record CreateParkingSpotsCommand(
    Guid FacilityId,
    IReadOnlyList<CreateParkingSpotRequest> Spots)
    : IRequestResult<CreateParkingSpotsCommand, CreateParkingSpotsResponse>;

public sealed class CreateParkingSpotsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingSpotsCommand, CreateParkingSpotsResponse>
{
    public async Task<Result<CreateParkingSpotsResponse>> Handle(CreateParkingSpotsCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreateParkingSpotsResponse>.Failure("Authentication required.", 401);

        if (request.Spots.Count == 0)
            return Result<CreateParkingSpotsResponse>.Failure("At least one spot is required.");

        var facilityId = request.FacilityId;

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Select(f => new { f.Id, f.ProviderId })
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility is null)
            return Result<CreateParkingSpotsResponse>.Failure("Parking facility not found.", 404);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Result<CreateParkingSpotsResponse>.Failure(
                "You must own the parking provider to add spots to this facility.", 403);

        var normalizedNumbers = new List<string>();
        var spotsToAdd = new List<ParkingSpot>();

        foreach (var spot in request.Spots)
        {
            if (!Enum.TryParse<VehicleTypeEnum>(spot.VehicleType, ignoreCase: true, out var vehicleType))
                return Result<CreateParkingSpotsResponse>.Failure("Invalid vehicle type.");

            var spotNumber = spot.SpotNumber.Trim().ToUpperInvariant();

            if (normalizedNumbers.Contains(spotNumber, StringComparer.Ordinal))
                return Result<CreateParkingSpotsResponse>.Failure(
                    $"Duplicate spot number '{spotNumber}' in the same request.", 400);

            normalizedNumbers.Add(spotNumber);

            spotsToAdd.Add(new ParkingSpot
            {
                FacilityId = facilityId,
                SpotNumber = spotNumber,
                VehicleType = vehicleType,
                PricePerHourNpr = spot.PricePerHourNpr,
                IsActive = spot.IsActive ?? true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        var existingNumbers = new HashSet<string>(
            await context.ParkingSpots
                .Where(s => s.FacilityId == facilityId)
                .Select(s => s.SpotNumber)
                .ToListAsync(cancellationToken), StringComparer.Ordinal);

        var conflicts = normalizedNumbers.Where(existingNumbers.Contains).ToList();
        if (conflicts.Count > 0)
            return Result<CreateParkingSpotsResponse>.Failure(
                $"Spot numbers already exist in this facility: {string.Join(", ", conflicts)}.", 409);

        context.ParkingSpots.AddRange(spotsToAdd);
        await context.SaveChangesAsync(cancellationToken);

        var items = spotsToAdd
            .Select(s => new ParkingSpotItemResponse(
                s.Id,
                s.SpotNumber,
                s.VehicleType,
                s.VehicleType.ToDescription(),
                s.PricePerHourNpr,
                s.IsActive))
            .ToList();

        var twoWheeler = spotsToAdd.Count(s => s.VehicleType == VehicleTypeEnum.TwoWheeler);
        var fourWheeler = spotsToAdd.Count(s => s.VehicleType == VehicleTypeEnum.FourWheeler);

        var existingCounts = await context.ParkingSpots
            .AsNoTracking()
            .Where(s => s.FacilityId == facilityId)
            .GroupBy(s => s.VehicleType)
            .Select(g => new { VehicleType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Result<CreateParkingSpotsResponse>.Success(
            new CreateParkingSpotsResponse(
                facilityId,
                items,
                existingCounts.Where(g => g.VehicleType == VehicleTypeEnum.TwoWheeler).Sum(g => g.Count),
                existingCounts.Where(g => g.VehicleType == VehicleTypeEnum.FourWheeler).Sum(g => g.Count)));
    }
}
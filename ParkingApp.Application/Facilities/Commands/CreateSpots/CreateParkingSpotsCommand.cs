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
    : IRequestResult<CreateParkingSpotsCommand, Unit>;

public sealed class CreateParkingSpotsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingSpotsCommand, Unit>
{
    public async Task<Result<Unit>> Handle(CreateParkingSpotsCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Unit>.Failure("Authentication required.", 401);

        if (request.Spots.Count == 0)
            return Result<Unit>.Failure("At least one spot is required.");

        var facilityId = request.FacilityId;

        var facility = await context.ParkingFacilities
            .AsNoTracking()
            .Select(f => new { f.Id, f.ProviderId })
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility is null)
            return Result<Unit>.Failure("Parking facility not found.", 404);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, facility.ProviderId, cancellationToken))
            return Result<Unit>.Failure(
                "You must own the parking provider to add spots to this facility.", 403);

        var normalizedNumbers = new List<string>();
        var spotsToAdd = new List<ParkingSpot>();

        foreach (var spot in request.Spots)
        {
            if (!Enum.TryParse<VehicleTypeEnum>(spot.VehicleType, ignoreCase: true, out var vehicleType))
                return Result<Unit>.Failure("Invalid vehicle type.");

            var spotNumber = spot.SpotNumber.Trim().ToUpperInvariant();

            if (normalizedNumbers.Contains(spotNumber, StringComparer.Ordinal))
                return Result<Unit>.Failure(
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
            return Result<Unit>.Failure(
                $"Spot numbers already exist in this facility: {string.Join(", ", conflicts)}.", 409);

        context.ParkingSpots.AddRange(spotsToAdd);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Facilities.Commands.Create;

public sealed record CreateParkingFacilityCommand(
    Guid ProviderId,
    string Name,
    string? Description,
    string Address,
    double? Latitude,
    double? Longitude)
    : IRequestResult<CreateParkingFacilityCommand, CreateParkingFacilityResponse>;

public sealed class CreateParkingFacilityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingFacilityCommand, CreateParkingFacilityResponse>
{
    public async Task<Result<CreateParkingFacilityResponse>> Handle(CreateParkingFacilityCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<CreateParkingFacilityResponse>.Failure("Authentication required.", 401);

        if (!await ProviderOwnership.IsOwnerAsync(context, userId.Value, request.ProviderId, cancellationToken))
            return Result<CreateParkingFacilityResponse>.Failure(
                "You must own the parking provider to add a facility.", 403);

        var facility = new ParkingFacility
        {
            ProviderId = request.ProviderId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.ParkingFacilities.Add(facility);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateParkingFacilityResponse>.Success(
            new CreateParkingFacilityResponse(
                facility.Id,
                facility.ProviderId,
                facility.Name,
                facility.Description,
                facility.Address,
                facility.Latitude,
                facility.Longitude,
                facility.ApprovalStatus,
                facility.ApprovalStatus.ToDescription(),
                TwoWheelerCount: 0,
                FourWheelerCount: 0));
    }
}
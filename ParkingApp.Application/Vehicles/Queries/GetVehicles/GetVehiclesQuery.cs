using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Vehicles.Queries.GetVehicles;

public sealed record GetVehiclesQuery() : IRequestResult<GetVehiclesQuery, GetVehiclesResponse>;

public sealed class GetVehiclesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetVehiclesQuery, GetVehiclesResponse>
{
    public async Task<Result<GetVehiclesResponse>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetVehiclesResponse>.Failure("Authentication required.", 401);

        var vehicles = await context.Vehicles
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new { v.Id, v.VehicleType, v.Name, v.VehicleNumber })
            .ToListAsync(cancellationToken);

        var items = vehicles
            .Select(v => new VehicleItemResponse(
                v.Id,
                v.VehicleType,
                v.VehicleType.ToDescription(),
                v.Name,
                v.VehicleNumber))
            .ToList();

        return Result<GetVehiclesResponse>.Success(
            new GetVehiclesResponse(items, items.Count > 0));
    }
}
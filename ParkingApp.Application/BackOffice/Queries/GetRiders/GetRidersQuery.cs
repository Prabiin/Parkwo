using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetRiders;

public sealed record GetRidersQuery(VehicleTypeEnum? VehicleType)
    : IRequestResult<GetRidersQuery, GetRidersResponse>;

public sealed class GetRidersQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetRidersQuery, GetRidersResponse>
{
    public async Task<Result<GetRidersResponse>> Handle(
        GetRidersQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (request.VehicleType.HasValue)
        {
            var vehicleType = request.VehicleType.Value;
            query = query.Where(u => u.Vehicles.Any(v => v.VehicleType == vehicleType));
        }

        var riders = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.PhoneNumber,
                u.Email,
                u.CreatedAtUtc,
                u.IsPhoneVerified,
                VehicleTypes = u.Vehicles.Select(v => v.VehicleType).ToList()
            })
            .ToListAsync(cancellationToken);

        var items = riders
            .Select(r =>
            {
                var isProfileComplete = !string.IsNullOrWhiteSpace(r.FullName)
                                        && !string.IsNullOrWhiteSpace(r.Email);

                return new RiderItemResponse(
                    r.Id,
                    r.FullName,
                    r.PhoneNumber,
                    r.Email,
                    r.IsPhoneVerified,
                    isProfileComplete,
                    r.CreatedAtUtc,
                    r.VehicleTypes.Count,
                    r.VehicleTypes.Select(t => new VehicleTypeItemResponse(
                            t,
                            t.ToDescription()))
                        .ToList());
            })
            .ToList();

        return Result<GetRidersResponse>.Success(
            new GetRidersResponse(items));
    }
}
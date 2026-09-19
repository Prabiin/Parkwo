using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Features.Shared;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Profile.Queries.GetProfile;

public sealed record GetProfileQuery() : IRequestResult<GetProfileQuery, GetProfileResponse>;

public sealed class GetProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetProfileQuery, GetProfileResponse>
{
    public async Task<Result<GetProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetProfileResponse>.Failure("Authentication required.", 401);

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<GetProfileResponse>.Failure("User not found.", 404);

        var hasVehicle = await context.Vehicles
            .AnyAsync(v => v.UserId == user.Id, cancellationToken);

        return Result<GetProfileResponse>.Success(
            new GetProfileResponse(
                user.FullName,
                user.PhoneNumber,
                user.Email,
                user.Gender,
                user.Gender?.ToDescription(),
                user.DateOfBirth,
                user.CreatedAtUtc,
                AuthDbHelper.IsProfileComplete(user),
                hasVehicle,
                BookingsCount: 0,
                AmountSavedInNpr: 0m,
                Rating: 0m,
                ProfileImageUrl: user.ProfileImageUrl));
    }
}
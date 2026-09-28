using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Licenses.Queries.GetDrivingLicense;

public sealed record GetDrivingLicenseQuery() : IRequestResult<GetDrivingLicenseQuery, DrivingLicenseResponse?>;

public sealed class GetDrivingLicenseQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<GetDrivingLicenseQuery, DrivingLicenseResponse?>
{
    public async Task<Result<DrivingLicenseResponse?>> Handle(GetDrivingLicenseQuery request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<DrivingLicenseResponse?>.Failure("Authentication required.", 401);

        var license = await context.DrivingLicenses
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (license is null)
            return Result<DrivingLicenseResponse?>.Failure("No driving license submitted on your account.", 404);

        return Result<DrivingLicenseResponse?>.Success(new DrivingLicenseResponse(
            license.Id,
            license.LicenseNumber,
            license.Categories,
            license.Categories.Select(c => c.ToDescription()).ToList(),
            license.FrontImageUrl,
            license.BackImageUrl,
            license.ExpiryDate,
            license.ApprovalStatus,
            license.ApprovalStatus.ToDescription(),
            license.RejectionReason));
    }
}

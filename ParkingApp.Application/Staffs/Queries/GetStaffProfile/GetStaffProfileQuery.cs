using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Queries.GetStaffProfile;

/// <summary>
/// The staff app's own profile: who is signed in, which facility they are
/// posted at, and whether they still owe a password change. Kept reachable
/// before the forced change so the app can show the prompt with real data.
/// </summary>
public sealed record GetStaffProfileQuery
    : IRequestResult<GetStaffProfileQuery, GetStaffProfileResponse>;

public sealed record GetStaffProfileResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string Email,
    DateOnly? DateOfBirth,
    int? Gender,
    string? GenderDescription,
    string? DeviceName,
    string? DeviceBrand,
    Guid FacilityId,
    string FacilityName,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAtUtc);

public sealed class GetStaffProfileQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetStaffProfileQuery, GetStaffProfileResponse>
{
    public async Task<Result<GetStaffProfileResponse>> Handle(
        GetStaffProfileQuery request,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.StaffId;
        if (staffId is null)
            return Result<GetStaffProfileResponse>.Failure("Authentication required.", 401);

        var staff = await context.Staffs
            .AsNoTracking()
            .Where(s => s.Id == staffId.Value && !s.IsDeleted && !s.MustChangePassword)
            .Select(s => new
            {
                s.Id,
                s.FullName,
                s.PhoneNumber,
                s.Email,
                s.DateOfBirth,
                s.Gender,
                s.DeviceName,
                s.DeviceBrand,
                s.FacilityId,
                FacilityName = s.Facility != null ? s.Facility.Name : string.Empty,
                s.MustChangePassword,
                s.LastLoginAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (staff is null)
            return Result<GetStaffProfileResponse>.Failure("Staff member not found.", 404);

        return Result<GetStaffProfileResponse>.Success(
            new GetStaffProfileResponse(
                staff.Id,
                staff.FullName,
                staff.PhoneNumber,
                staff.Email,
                staff.DateOfBirth,
                staff.Gender is null ? null : (int)staff.Gender.Value,
                staff.Gender?.ToDescription(),
                staff.DeviceName,
                staff.DeviceBrand,
                staff.FacilityId,
                staff.FacilityName,
                staff.MustChangePassword,
                staff.LastLoginAtUtc));
    }
}

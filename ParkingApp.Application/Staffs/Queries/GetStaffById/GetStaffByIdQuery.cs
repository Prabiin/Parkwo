using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Queries.GetStaffById;

/// <summary>
/// BackOffice staff detail: the full record behind the list row, including the
/// facility name and the device metadata captured for asset tracking.
/// </summary>
public sealed record GetStaffByIdQuery(Guid StaffId)
    : IRequestResult<GetStaffByIdQuery, GetStaffByIdResponse>;

public sealed record GetStaffByIdResponse(
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
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed class GetStaffByIdQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetStaffByIdQuery, GetStaffByIdResponse>
{
    public async Task<Result<GetStaffByIdResponse>> Handle(
        GetStaffByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var staff = await context.Staffs
            .AsNoTracking()
            .Where(s => s.Id == request.StaffId && !s.IsDeleted)
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
                s.IsActive,
                s.MustChangePassword,
                s.LastLoginAtUtc,
                s.CreatedAtUtc,
                s.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (staff is null)
            return Result<GetStaffByIdResponse>.Failure("Staff member not found.", 404);

        return Result<GetStaffByIdResponse>.Success(
            new GetStaffByIdResponse(
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
                staff.IsActive,
                staff.MustChangePassword,
                staff.LastLoginAtUtc,
                staff.CreatedAtUtc,
                staff.UpdatedAtUtc));
    }
}

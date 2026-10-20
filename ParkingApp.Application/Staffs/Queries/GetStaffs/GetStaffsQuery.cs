using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Queries.GetStaffs;

/// <summary>
/// BackOffice staff list. Every filter is optional and they compose:
/// <c>facilityId</c> narrows to one lot, <c>search</c> matches name/email/phone,
/// and <c>status</c> picks Active, Inactive or All. Soft-deleted staff never
/// appear — a deleted gate operator is gone from the roster, though their
/// historic gate records remain.
/// </summary>
public enum StaffStatusFilterEnum
{
    [Description("All")]
    All = 0,

    [Description("Active")]
    Active = 1,

    [Description("Inactive")]
    Inactive = 2
}

public sealed record GetStaffsQuery(
    Guid? FacilityId,
    string? Search,
    StaffStatusFilterEnum Status)
    : IRequestResult<GetStaffsQuery, GetStaffsResponse>;

public sealed record GetStaffsResponse(IReadOnlyList<StaffListItem> Items);

public sealed record StaffListItem(
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
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    /// <summary>
    /// True while the account is still on a Parkwo-generated password, i.e. the
    /// staff member has never signed in and set their own. Drives the BackOffice
    /// "resend invite" action, which is only offered while this is true.
    /// </summary>
    bool MustChangePassword);

public sealed class GetStaffsQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetStaffsQuery, GetStaffsResponse>
{
    public async Task<Result<GetStaffsResponse>> Handle(
        GetStaffsQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Staffs
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (request.FacilityId is { } facilityId)
            query = query.Where(s => s.FacilityId == facilityId);

        switch (request.Status)
        {
            case StaffStatusFilterEnum.Active:
                query = query.Where(s => s.IsActive);
                break;
            case StaffStatusFilterEnum.Inactive:
                query = query.Where(s => !s.IsActive);
                break;
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(term)
                || s.Email.ToLower().Contains(term)
                || s.PhoneNumber.Contains(term));
        }

        var rows = await query
            .OrderBy(s => s.FullName)
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
                s.LastLoginAtUtc,
                s.CreatedAtUtc,
                s.MustChangePassword
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(s => new StaffListItem(
                s.Id,
                s.FullName,
                s.PhoneNumber,
                s.Email,
                s.DateOfBirth,
                s.Gender is null ? null : (int)s.Gender.Value,
                s.Gender?.ToDescription(),
                s.DeviceName,
                s.DeviceBrand,
                s.FacilityId,
                s.FacilityName,
                s.IsActive,
                s.LastLoginAtUtc,
                s.CreatedAtUtc,
                s.MustChangePassword))
            .ToList();

        return Result<GetStaffsResponse>.Success(new GetStaffsResponse(items));
    }
}

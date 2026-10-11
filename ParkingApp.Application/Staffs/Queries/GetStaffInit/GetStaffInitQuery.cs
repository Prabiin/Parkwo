using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Common.Models;
using ParkingApp.Application.Staffs.Queries.GetStaffs;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Staffs.Queries.GetStaffInit;

/// <summary>
/// Dropdown data for the BackOffice staff form: the facilities a staff member
/// can be assigned to, the gender options and the status filters. Facilities are
/// returned as id/name pairs because the form posts the id back.
/// </summary>
public sealed record GetStaffInitQuery
    : IRequestResult<GetStaffInitQuery, GetStaffInitResponse>;

public sealed record GetStaffInitResponse(
    IReadOnlyList<FacilityOption> Facilities,
    IReadOnlyList<ListModel<GenderEnum>> Genders,
    IReadOnlyList<ListModel<StaffStatusFilterEnum>> Statuses);

public sealed record FacilityOption(Guid Id, string Name);

public sealed class GetStaffInitQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetStaffInitQuery, GetStaffInitResponse>
{
    public async Task<Result<GetStaffInitResponse>> Handle(
        GetStaffInitQuery request,
        CancellationToken cancellationToken = default)
    {
        var facilities = await context.ParkingFacilities
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FacilityOption(f.Id, f.Name))
            .ToListAsync(cancellationToken);

        return Result<GetStaffInitResponse>.Success(
            new GetStaffInitResponse(
                facilities,
                ListModel<GenderEnum>.FromEnum(),
                ListModel<StaffStatusFilterEnum>.FromEnum()));
    }
}

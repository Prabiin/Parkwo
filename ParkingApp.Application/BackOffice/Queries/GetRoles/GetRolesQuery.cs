using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Queries.GetRoles;

public sealed record GetRolesQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 20)
    : IRequestResult<GetRolesQuery, GetRolesResponse>;

public sealed class GetRolesQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetRolesQuery, GetRolesResponse>
{
    public async Task<Result<GetRolesResponse>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken = default)
    {
        // Deleted roles are gone from the console. Inactive ones are not: they
        // still exist and can be switched back on, so they stay in the list.
        var query = context.BackOfficeRoles.AsNoTracking()
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(term)
                || (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RoleItemResponse(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem,
                r.IsActive,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<GetRolesResponse>.Success(
            new GetRolesResponse(roles, totalCount, request.Page, request.PageSize));
    }
}
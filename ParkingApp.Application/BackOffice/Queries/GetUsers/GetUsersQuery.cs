using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Queries.GetUsers;

public sealed record GetUsersQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20)
    : IRequestResult<GetUsersQuery, GetUsersResponse>;

public sealed class GetUsersQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetUsersQuery, GetUsersResponse>
{
    public async Task<Result<GetUsersResponse>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.BackOfficeUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(u => u.UserName.ToLower().Contains(term)
                || u.FullName.ToLower().Contains(term)
                || u.Email.ToLower().Contains(term));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserItemResponse(
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsActive,
                u.LastLoginAtUtc,
                u.CreatedAtUtc,
                u.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<GetUsersResponse>.Success(
            new GetUsersResponse(users, totalCount, request.Page, request.PageSize));
    }
}
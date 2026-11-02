using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId)
    : IRequestResult<GetUserByIdQuery, GetUserByIdResponse>;

public sealed class GetUserByIdQueryHandler(IApplicationDbContext context)
    : IRequestResultHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    public async Task<Result<GetUserByIdResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .AsNoTracking()
            .Include(u => u.UserRole)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<GetUserByIdResponse>.Failure("User not found.", 404);

        UserRoleResponse? role = null;
        if (user.UserRole?.Role != null)
        {
            role = new UserRoleResponse(
                user.UserRole.Role.Id,
                user.UserRole.Role.Name,
                user.UserRole.Role.IsSystem);
        }

        return Result<GetUserByIdResponse>.Success(
            new GetUserByIdResponse(
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.IsActive,
                user.LastLoginAtUtc,
                user.CreatedAtUtc,
                user.UpdatedAtUtc,
                role));
    }
}
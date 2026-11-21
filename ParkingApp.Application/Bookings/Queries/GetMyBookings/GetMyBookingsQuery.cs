using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Bookings.Queries.Shared;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Bookings.Queries.GetMyBookings;

/// <summary>
/// The signed-in rider's own bookings, newest first for history and soonest
/// first for upcoming. Walk-ins never appear (they have no rider account) and
/// soft-deleted rows are hidden.
/// </summary>
public sealed record GetMyBookingsQuery(
    BookingListScopeEnum? Scope,
    int Page = 1,
    int PageSize = 20)
    : IRequestResult<GetMyBookingsQuery, GetMyBookingsResponse>;

public sealed record GetMyBookingsResponse(
    IReadOnlyList<RiderBookingItemResponse> Bookings,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);

public sealed class GetMyBookingsQueryValidator : AbstractValidator<GetMyBookingsQuery>
{
    public const int MaxPageSize = 50;

    public GetMyBookingsQueryValidator()
    {
        RuleFor(x => x.Scope)
            .IsInEnum()
            .When(x => x.Scope.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}

public sealed class GetMyBookingsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestResultHandler<GetMyBookingsQuery, GetMyBookingsResponse>
{
    public async Task<Result<GetMyBookingsResponse>> Handle(
        GetMyBookingsQuery request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<GetMyBookingsResponse>.Failure("Authentication required.", 401);

        var now = DateTimeOffset.UtcNow;

        var mine = context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId && !b.IsDeleted);

        var filtered = request.Scope switch
        {
            BookingListScopeEnum.Upcoming => RiderBookings.Upcoming(mine, now)
                .OrderBy(b => b.StartsAtUtc),
            BookingListScopeEnum.History => RiderBookings.History(mine, now)
                .OrderByDescending(b => b.StartsAtUtc),
            _ => mine.OrderByDescending(b => b.StartsAtUtc)
        };

        var totalCount = await filtered.CountAsync(cancellationToken);

        var rows = await RiderBookings.Project(context, filtered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize))
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => RiderBookings.ToResponse(r, now)).ToList();

        return Result<GetMyBookingsResponse>.Success(new GetMyBookingsResponse(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            request.Page * request.PageSize < totalCount));
    }
}

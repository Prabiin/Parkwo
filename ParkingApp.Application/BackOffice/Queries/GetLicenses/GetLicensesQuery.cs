using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.BackOffice.Queries.GetLicenses;

public sealed record GetLicensesQuery(ApprovalStatusEnum? ApprovalStatus)
    : IRequestResult<GetLicensesQuery, GetLicensesResponse>;

public sealed class GetLicensesQueryHandler(IApplicationDbContext context, IImageUrlResolver imageUrls)
    : IRequestResultHandler<GetLicensesQuery, GetLicensesResponse>
{
    public async Task<Result<GetLicensesResponse>> Handle(
        GetLicensesQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.DrivingLicenses.AsNoTracking();

        if (request.ApprovalStatus.HasValue)
        {
            var approvalStatus = request.ApprovalStatus.Value;
            query = query.Where(d => d.ApprovalStatus == approvalStatus);
        }

        var licenses = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new
            {
                d.Id,
                d.UserId,
                UserFullName = d.User != null ? d.User.FullName : null,
                UserPhoneNumber = d.User != null ? d.User.PhoneNumber : null,
                d.LicenseNumber,
                d.Categories,
                d.FrontImageUrl,
                d.BackImageUrl,
                d.ExpiryDate,
                d.ApprovalStatus,
                d.RejectionReason,
                d.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = licenses
            .Select(d => new DrivingLicenseItemResponse(
                d.Id,
                d.UserId,
                d.UserFullName,
                d.UserPhoneNumber,
                d.LicenseNumber,
                d.Categories,
                d.Categories.Select(c => c.ToDescription()).ToList(),
                imageUrls.Resolve(d.FrontImageUrl)!,
                imageUrls.Resolve(d.BackImageUrl)!,
                d.ExpiryDate,
                d.ApprovalStatus,
                d.ApprovalStatus.ToDescription(),
                d.RejectionReason,
                d.CreatedAtUtc))
            .ToList();

        return Result<GetLicensesResponse>.Success(
            new GetLicensesResponse(items));
    }
}

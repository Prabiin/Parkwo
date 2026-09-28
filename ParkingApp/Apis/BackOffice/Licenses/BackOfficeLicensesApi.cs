using ParkingApp.Application.BackOffice.Commands.UpdateApproval;
using ParkingApp.Application.BackOffice.Queries.GetLicenses;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Licenses;

public class BackOfficeLicensesApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapGet("licenses", Licenses).RequireAuthorization(BackOfficePolicy);
        group.MapPut("licenses/{licenseId}/approval", UpdateDrivingLicenseApproval).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Licenses(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetLicensesQuery, GetLicensesResponse>(sender,
            new GetLicensesQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> UpdateDrivingLicenseApproval(ISender sender, IServiceProvider serviceProvider,
        Guid licenseId, UpdateDrivingLicenseApprovalCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateDrivingLicenseApprovalCommand, Unit>(sender,
            request with { LicenseId = licenseId }, serviceProvider, cancellationToken);
}

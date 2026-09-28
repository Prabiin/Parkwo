using ParkingApp.Application.BackOffice.Commands.UpdateApproval;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Facilities;

public class BackOfficeFacilitiesApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapGet("facilities", Facilities).RequireAuthorization(BackOfficePolicy);
        group.MapGet("facilities/{facilityId}", FacilityDetail).RequireAuthorization(BackOfficePolicy);
        group.MapPut("facilities/{facilityId}/approval", UpdateParkingFacilityApproval).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Facilities(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>(sender,
            new GetParkingFacilitiesQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> FacilityDetail(ISender sender, IServiceProvider serviceProvider,
        string facilityId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(facilityId, out var id))
            return Results.BadRequest(new { Errors = new[] { "facilityId: Invalid facility id." } });

        return await ExecuteQuery<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>(sender,
            new GetParkingFacilityDetailQuery(id), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> UpdateParkingFacilityApproval(ISender sender, IServiceProvider serviceProvider,
        Guid facilityId, UpdateParkingFacilityApprovalCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateParkingFacilityApprovalCommand, Unit>(sender,
            request with { FacilityId = facilityId }, serviceProvider, cancellationToken);
}

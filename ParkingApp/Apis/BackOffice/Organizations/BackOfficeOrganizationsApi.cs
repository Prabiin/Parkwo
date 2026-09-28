using ParkingApp.Application.BackOffice.Commands.UpdateApproval;
using ParkingApp.Application.BackOffice.Queries.GetOrganizations;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Organizations;

public class BackOfficeOrganizationsApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapGet("organizations", Organizations).RequireAuthorization(BackOfficePolicy);
        group.MapPut("organizations/{organizationId}/approval", UpdateOrganizationApproval).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Organizations(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetOrganizationsQuery, GetOrganizationsResponse>(sender,
            new GetOrganizationsQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> UpdateOrganizationApproval(ISender sender, IServiceProvider serviceProvider,
        Guid organizationId, UpdateOrganizationApprovalCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateOrganizationApprovalCommand, Unit>(sender,
            request with { OrganizationId = organizationId }, serviceProvider, cancellationToken);
}

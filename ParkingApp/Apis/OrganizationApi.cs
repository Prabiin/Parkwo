using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Organizations.Commands.Create;
using ParkingApp.Application.Organizations.Queries.GetMyOrganizations;

namespace ParkingApp.Api.Apis;

public class OrganizationApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup(this, "organizations")
            .MapPost(CreateOrganization, "", "")
            .MapGet(ListMyOrganizations, "", "")
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateOrganization(ISender sender, IServiceProvider serviceProvider,
        CreateOrganizationCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateOrganizationCommand, CreateOrganizationResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ListMyOrganizations(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetMyOrganizationsQuery, GetMyOrganizationsResponse>(sender,
            new GetMyOrganizationsQuery(), serviceProvider, cancellationToken);
}
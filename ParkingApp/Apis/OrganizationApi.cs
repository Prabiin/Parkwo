using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Organizations.Commands.Create;
using ParkingApp.Application.Organizations.Queries.GetOrganizations;

namespace ParkingApp.Api.Apis;

public class OrganizationApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("organizations")
            .MapPost(CreateOrganization, "", "")
            .MapGet(ListOrganizations, "", "")
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateOrganization(ISender sender, IServiceProvider serviceProvider,
        CreateOrganizationCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateOrganizationCommand, Guid>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ListOrganizations(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetOrganizationsQuery, GetOrganizationsResponse>(sender,
            new GetOrganizationsQuery(), serviceProvider, cancellationToken);
}
using ParkingApp.Application.BackOffice.Queries.GetParkingProviders;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Providers;

public class BackOfficeProvidersApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapGet("parking-providers", ParkingProviders).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> ParkingProviders(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetParkingProvidersQuery, GetParkingProvidersResponse>(sender,
            new GetParkingProvidersQuery(), serviceProvider, cancellationToken);
}

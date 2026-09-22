using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.ParkingProviders.Commands.Create;
using ParkingApp.Application.ParkingProviders.Queries.GetMyParkingProviders;

namespace ParkingApp.Api.Apis;

public class ParkingProviderApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("parking-providers")
            .MapPost(CreateParkingProvider, "", "")
            .MapGet(ListMyParkingProviders, "", "")
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateParkingProvider(ISender sender, IServiceProvider serviceProvider,
        CreateParkingProviderCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateParkingProviderCommand, Guid>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ListMyParkingProviders(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetMyParkingProvidersQuery, GetMyParkingProvidersResponse>(sender,
            new GetMyParkingProvidersQuery(), serviceProvider, cancellationToken);
}
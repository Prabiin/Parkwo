using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Application.ParkingProviders.Commands.Create;
using ParkingApp.Application.ParkingProviders.Queries.GetParkingProviders;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis;

public class ParkingProviderApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("parking-providers")
            .MapPost(CreateParkingProvider, "", "")
            .MapGet(ListParkingProviders, "", "")
            .MapGet(GetParkingProvidersInit, "init", "")
            .RequireAuthorization();
    }

    private static IResult GetParkingProvidersInit()
        => Results.Ok(new { ProviderTypes = ListModel<ProviderTypeEnum>.FromEnum() });

    private static async Task<IResult> CreateParkingProvider(ISender sender, IServiceProvider serviceProvider,
        CreateParkingProviderCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateParkingProviderCommand, Guid>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> ListParkingProviders(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetParkingProvidersQuery, GetParkingProvidersResponse>(sender,
            new GetParkingProvidersQuery(), serviceProvider, cancellationToken);
}
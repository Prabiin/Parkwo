using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Vehicles.Commands.Create;
using ParkingApp.Application.Vehicles.Queries.GetVehicles;

namespace ParkingApp.Api.Apis;

public class VehicleApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("vehicles")
            .MapGet(ListVehicles, "", "")
            .MapPost(CreateVehicle, "", "")
            .RequireAuthorization();
    }

    private static async Task<IResult> ListVehicles(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetVehiclesQuery, GetVehiclesResponse>(sender,
            new GetVehiclesQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateVehicle(ISender sender, IServiceProvider serviceProvider,
        CreateVehicleCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateVehicleCommand, CreateVehicleResponse>(sender,
            request, serviceProvider, cancellationToken);
}
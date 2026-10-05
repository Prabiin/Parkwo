using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Application.Vehicles.Commands.Create;
using ParkingApp.Application.Vehicles.Queries.GetVehicles;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

public class VehicleApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("vehicles")
            .MapGet(ListVehicles, "", "")
            .MapGet(GetVehiclesInit, "init", "")
            .MapPost(CreateVehicle, "", "")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);
    }

    private static IResult GetVehiclesInit()
        => Results.Ok(new
        {
            VehicleTypes = ListModel<VehicleTypeEnum>.FromEnum(),
            VehicleCategories = ListModel<VehicleCategoryEnum>.FromEnum()
        });

    private static async Task<IResult> ListVehicles(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetVehiclesQuery, GetVehiclesResponse>(sender,
            new GetVehiclesQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateVehicle(ISender sender, IServiceProvider serviceProvider,
        CreateVehicleCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateVehicleCommand, Guid>(sender,
            request, serviceProvider, cancellationToken);
}

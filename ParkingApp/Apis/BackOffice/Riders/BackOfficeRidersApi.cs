using ParkingApp.Application.BackOffice.Queries.GetRiders;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Apis.BackOffice.Riders;

public class BackOfficeRidersApi : BackOfficeGroup
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapGet("riders", Riders).RequireAuthorization(BackOfficePolicy);
    }

    private static async Task<IResult> Riders(ISender sender, IServiceProvider serviceProvider,
        string? vehicleType, CancellationToken cancellationToken)
    {
        if (!TryParseVehicleType(vehicleType, out var parsedVehicleType))
            return Results.BadRequest(new { Errors = new[] { "vehicleType: Invalid vehicle type." } });

        return await ExecuteQuery<GetRidersQuery, GetRidersResponse>(sender,
            new GetRidersQuery(parsedVehicleType), serviceProvider, cancellationToken);
    }
}

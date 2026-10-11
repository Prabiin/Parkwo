using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.FreeParkings.Queries.GetPublicFreeParking;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

/// <summary>
/// Rider-facing read for Parkwo's public free parkings. Free parkings are never
/// bookable — they only appear as markers on the nearby map, and this endpoint
/// backs the "tap a marker to view detail" screen.
/// </summary>
public class FreeParkingApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("free-parkings")
            .MapGet(GetById, "{freeParkingId}", "")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);
    }

    private static async Task<IResult> GetById(ISender sender, IServiceProvider serviceProvider,
        Guid freeParkingId, CancellationToken cancellationToken)
        => await ExecuteQuery<GetPublicFreeParkingQuery, GetPublicFreeParkingResponse>(sender,
            new GetPublicFreeParkingQuery(freeParkingId), serviceProvider, cancellationToken);
}

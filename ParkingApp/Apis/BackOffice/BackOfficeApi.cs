using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.BackOffice.Commands.Login;
using ParkingApp.Application.BackOffice.Queries.GetOrganizations;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;
using ParkingApp.Application.BackOffice.Queries.GetParkingProviders;
using ParkingApp.Application.BackOffice.Queries.GetRiders;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis.BackOffice;

public class BackOfficeApi : EndpointGroupBase
{
    public const string BackOfficePolicy = "BackOfficeOnly";

    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("backoffice");

        group.MapPost("auth/login", Login);
        group.MapGet("init", Init).RequireAuthorization(BackOfficePolicy);
        group.MapGet("riders", Riders).RequireAuthorization(BackOfficePolicy);
        group.MapGet("organizations", Organizations).RequireAuthorization(BackOfficePolicy);
        group.MapGet("parking-providers", ParkingProviders).RequireAuthorization(BackOfficePolicy);
        group.MapGet("facilities", Facilities).RequireAuthorization(BackOfficePolicy);
        group.MapGet("facilities/{facilityId}", FacilityDetail).RequireAuthorization(BackOfficePolicy);
    }

    private static IResult Init()
        => Results.Ok(new
        {
            VehicleTypes = ListModel<VehicleTypeEnum>.FromEnum(),
            ApprovalStatuses = ListModel<ApprovalStatusEnum>.FromEnum()
        });

    private static async Task<IResult> Login(ISender sender, IServiceProvider serviceProvider,
        BackOfficeLoginCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<BackOfficeLoginCommand, BackOfficeLoginResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> Riders(ISender sender, IServiceProvider serviceProvider,
        string? vehicleType, CancellationToken cancellationToken)
    {
        if (!TryParseVehicleType(vehicleType, out var parsedVehicleType))
            return Results.BadRequest(new { Errors = new[] { "vehicleType: Invalid vehicle type." } });

        return await ExecuteQuery<GetRidersQuery, GetRidersResponse>(sender,
            new GetRidersQuery(parsedVehicleType), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> Organizations(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetOrganizationsQuery, GetOrganizationsResponse>(sender,
            new GetOrganizationsQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> ParkingProviders(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetParkingProvidersQuery, GetParkingProvidersResponse>(sender,
            new GetParkingProvidersQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> Facilities(ISender sender, IServiceProvider serviceProvider,
        string? approvalStatus, CancellationToken cancellationToken)
    {
        if (!TryParseApprovalStatus(approvalStatus, out var parsedStatus))
            return Results.BadRequest(new { Errors = new[] { "approvalStatus: Invalid approval status." } });

        return await ExecuteQuery<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>(sender,
            new GetParkingFacilitiesQuery(parsedStatus), serviceProvider, cancellationToken);
    }

    private static async Task<IResult> FacilityDetail(ISender sender, IServiceProvider serviceProvider,
        string facilityId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(facilityId, out var id))
            return Results.BadRequest(new { Errors = new[] { "facilityId: Invalid facility id." } });

        return await ExecuteQuery<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>(sender,
            new GetParkingFacilityDetailQuery(id), serviceProvider, cancellationToken);
    }

    private static bool TryParseVehicleType(string? value, out VehicleTypeEnum? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            vehicleType = null;
            return true;
        }

        if (int.TryParse(value, out var number) && Enum.IsDefined(typeof(VehicleTypeEnum), number))
        {
            vehicleType = (VehicleTypeEnum)number;
            return true;
        }

        vehicleType = null;
        return false;
    }

    private static bool TryParseApprovalStatus(string? value, out ApprovalStatusEnum? approvalStatus)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            approvalStatus = null;
            return true;
        }

        if (int.TryParse(value, out var number) && Enum.IsDefined(typeof(ApprovalStatusEnum), number))
        {
            approvalStatus = (ApprovalStatusEnum)number;
            return true;
        }

        approvalStatus = null;
        return false;
    }
}
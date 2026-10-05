using Microsoft.AspNetCore.Mvc;
using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Application.Licenses.Commands.Create;
using ParkingApp.Application.Licenses.Queries.GetDrivingLicense;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

public class LicenseApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("licenses")
            .MapGet(GetLicense, "", "")
            .MapGet(GetLicensesInit, "init", "")
            .MapPost(SubmitLicense, "", "", disableAntiforgery: true)
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);
    }

    private static IResult GetLicensesInit()
        => Results.Ok(new { LicenseCategories = ListModel<LicenseCategoryEnum>.FromEnum() });

    private static async Task<IResult> GetLicense(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetDrivingLicenseQuery, DrivingLicenseResponse?>(sender,
            new GetDrivingLicenseQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> SubmitLicense([FromForm] SubmitLicenseRequest request,
        ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteCommand<CreateDrivingLicenseCommand, Guid>(sender,
            new CreateDrivingLicenseCommand(
                request.LicenseNumber,
                request.Categories,
                request.ExpiryDate,
                ToUpload(request.Front),
                ToUpload(request.Back)),
            serviceProvider, cancellationToken);

    private static LicenseFileUpload? ToUpload(IFormFile? file)
        => file is null
            ? null
            : new LicenseFileUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream());
}

/// <summary>
/// Wire shape of the multipart license form. Bound with [FromForm] so the
/// OpenAPI document describes the five fields — without an explicit form-bound
/// parameter, swagger/v1/swagger.json emits no request body and Swagger UI
/// offers the mobile team no inputs or file pickers to test against.
/// </summary>
public sealed class SubmitLicenseRequest
{
    public string LicenseNumber { get; set; } = string.Empty;

    public string Categories { get; set; } = string.Empty;

    public string ExpiryDate { get; set; } = string.Empty;

    public IFormFile? Front { get; set; }

    public IFormFile? Back { get; set; }
}

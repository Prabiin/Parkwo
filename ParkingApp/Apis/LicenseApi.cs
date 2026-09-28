using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Models;
using ParkingApp.Application.Licenses.Commands.Create;
using ParkingApp.Application.Licenses.Queries.GetDrivingLicense;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis;

public class LicenseApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("licenses")
            .MapGet(GetLicense, "", "")
            .MapGet(GetLicensesInit, "init", "")
            .MapPost(SubmitLicense, "", "", disableAntiforgery: true)
            .RequireAuthorization();
    }

    private static IResult GetLicensesInit()
        => Results.Ok(new { LicenseCategories = ListModel<LicenseCategoryEnum>.FromEnum() });

    private static async Task<IResult> GetLicense(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetDrivingLicenseQuery, DrivingLicenseResponse?>(sender,
            new GetDrivingLicenseQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> SubmitLicense(HttpRequest httpRequest,
        ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.HasFormContentType)
            return Results.BadRequest(new
            {
                Errors = new[] { "Multipart/form-data with licenseNumber, categories, expiryDate, front, back is required." }
            });

        var form = await httpRequest.ReadFormAsync(cancellationToken);

        return await ExecuteCommand<CreateDrivingLicenseCommand, Guid>(sender,
            new CreateDrivingLicenseCommand(
                form["licenseNumber"].ToString(),
                form["categories"].ToString(),
                form["expiryDate"].ToString(),
                ToUpload(form.Files.GetFile("front")),
                ToUpload(form.Files.GetFile("back"))),
            serviceProvider, cancellationToken);
    }

    private static LicenseFileUpload? ToUpload(IFormFile? file)
        => file is null
            ? null
            : new LicenseFileUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream());
}

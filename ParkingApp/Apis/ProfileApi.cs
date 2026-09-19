using Microsoft.EntityFrameworkCore;
using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Profile.Commands.Update;
using ParkingApp.Application.Profile.Queries.GetProfile;

namespace ParkingApp.Api.Apis;

public class ProfileApi : EndpointGroupBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("profile")
            .MapGet(GetProfile, "", "")
            .MapPut(UpdateProfile, "", "")
            .MapPost(UploadPicture, "picture", "", disableAntiforgery: true)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetProfile(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => await ExecuteQuery<GetProfileQuery, GetProfileResponse>(sender,
            new GetProfileQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> UpdateProfile(ISender sender, IServiceProvider serviceProvider,
        UpdateProfileCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<UpdateProfileCommand, UpdateProfileResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> UploadPicture(IFormFile file,
        IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Results.NotFound();

        if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return Results.BadRequest(new
            {
                Errors = new[] { $"file: '{file.FileName}' is not a supported image type." }
            });

        if (file.Length > MaxImageBytes || file.Length == 0)
            return Results.BadRequest(new
            {
                Errors = new[] { $"file: '{file.FileName}' must be between 1 byte and 5 MB." }
            });

        await using var stream = file.OpenReadStream();
        var stored = await storage.SaveAsync(stream, file.FileName, file.ContentType, "profile-images", cancellationToken);

        var previousUrl = user.ProfileImageUrl;
        user.ProfileImageUrl = stored.Url;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousUrl) && previousUrl != stored.Url)
        {
            try
            {
                await storage.DeleteAsync(previousUrl, cancellationToken);
            }
            catch
            {
                // Best effort: the profile already points at the new picture.
            }
        }

        return Results.Ok(new { ProfileImageUrl = user.ProfileImageUrl });
    }
}
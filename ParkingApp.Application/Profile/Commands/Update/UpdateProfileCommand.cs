using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Features.Shared;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Profile.Commands.Update;

public sealed record UpdateProfileCommand(
    string FullName,
    string PhoneNumber,
    string Email,
    GenderEnum Gender,
    DateOnly DateOfBirth)
    : IRequestResult<UpdateProfileCommand, UpdateProfileResponse>;

public sealed class UpdateProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken = default)
    {
        var tokenUserId = currentUser.UserId;
        if (tokenUserId is null)
            return Result<UpdateProfileResponse>.Failure("Authentication required.", 401);

        // Onboarding targets the "dirty" row created at OTP time, identified by the
        // phone number in the request (auto-populated + locked in the mobile form).
        // A changed/unknown number finds no row -> 404.
        var normalizedPhone = request.PhoneNumber.Trim();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);

        if (user is null)
            return Result<UpdateProfileResponse>.Failure("User not found.", 404);

        // Guard: the request number must belong to the token owner. The mobile form
        // locks this field, so a mismatch can only be deliberate tampering -> 403.
        if (user.Id != tokenUserId.Value)
            return Result<UpdateProfileResponse>.Failure(
                "Phone number does not match the authenticated account.", 403);

        var normalizedEmail = request.Email.Trim();
        var emailTaken = await context.Users
            .AnyAsync(u => u.Id != user.Id && u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
            return Result<UpdateProfileResponse>.Failure("A user with this email already exists.", 409);

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<UpdateProfileResponse>.Success(
            new UpdateProfileResponse(
                user.FullName,
                user.PhoneNumber,
                user.Email,
                user.Gender!.Value,
                user.Gender!.Value.ToDescription(),
                user.DateOfBirth!.Value,
                AuthDbHelper.IsProfileComplete(user),
                user.ProfileImageUrl));
    }
}
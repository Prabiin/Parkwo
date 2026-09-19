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
    GenderEnum? Gender,
    DateOnly? DateOfBirth)
    : IRequestResult<UpdateProfileCommand, UpdateProfileResponse>;

public sealed class UpdateProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<UpdateProfileResponse>.Failure("Authentication required.", 401);

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<UpdateProfileResponse>.Failure("User not found.", 404);

        var normalizedEmail = request.Email.Trim();
        var emailTaken = await context.Users
            .AnyAsync(u => u.Id != userId && u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
            return Result<UpdateProfileResponse>.Failure("A user with this email already exists.", 409);

        var normalizedPhone = request.PhoneNumber.Trim();
        var phoneTaken = await context.Users
            .AnyAsync(u => u.Id != userId && u.PhoneNumber == normalizedPhone, cancellationToken);

        if (phoneTaken)
            return Result<UpdateProfileResponse>.Failure("A user with this phone number already exists.", 409);

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = normalizedPhone;
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
                user.Gender,
                user.Gender?.ToDescription(),
                user.DateOfBirth,
                AuthDbHelper.IsProfileComplete(user),
                user.ProfileImageUrl));
    }
}
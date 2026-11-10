using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.BackOffice;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.CreateUser;

/// <summary>
/// BackOffice creates an admin console account. The password is generated and
/// emailed to the user's address; <see cref="BackOfficeUser.MustChangePassword"/>
/// is left true, so the generated secret only ever opens the change-password
/// screen. The account is created whether or not the email goes out — a failed
/// send is recovered by resending the invite, which issues a fresh password.
/// </summary>
public sealed record CreateUserCommand(
    string UserName,
    string FullName,
    string Email,
    Guid RoleId)
    : IRequestResult<CreateUserCommand, CreateUserResponse>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(500)
            .EmailAddress();

        RuleFor(x => x.RoleId)
            .NotEmpty();
    }
}

public sealed class CreateUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<CreateUserCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestResultHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = request.UserName.Trim().ToLower();
        var normalizedEmail = request.Email.Trim().ToLower();

        // Check if username already exists
        var existingUser = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);

        if (existingUser is not null)
            return Result<CreateUserResponse>.Failure("Username already exists.", 400);

        // Check if email already exists
        var existingEmail = await context.BackOfficeUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingEmail is not null)
            return Result<CreateUserResponse>.Failure("Email already exists.", 400);

        // Validate role ID exists
        var roleExists = await context.BackOfficeRoles
            .AnyAsync(r => r.Id == request.RoleId, cancellationToken);

        if (!roleExists)
            return Result<CreateUserResponse>.Failure("Invalid role ID.", 400);

        var password = PasswordGenerator.Generate();

        var user = new BackOfficeUser
        {
            UserName = normalizedUserName,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            MustChangePassword = true,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = currentUser.UserId
        };

        // Assign single role (1:1)
        user.UserRole = new BackOfficeUserRole
        {
            RoleId = request.RoleId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Persist first, then try to deliver. An undelivered password is NOT a
        // reason to throw the account away: the row is what the resend action
        // acts on, and recreating the same person would be refused as a
        // duplicate email. A failed send is recovered by resending the invite,
        // which issues a fresh password (only the hash is stored, so the lost
        // one cannot be recalled).
        context.BackOfficeUsers.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var deliveryStatus = await BackOfficeUserPasswordDelivery.SendAsync(
            emailSender,
            logger,
            user,
            "Your Parkwo admin account",
            BackOfficeUserEmailTemplates.Welcome(user.FullName, user.UserName, user.Email, password),
            cancellationToken);

        return Result<CreateUserResponse>.Success(
            new CreateUserResponse(
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.MustChangePassword,
                !BackOfficeUserPasswordDelivery.IsFailed(deliveryStatus)));
    }
}

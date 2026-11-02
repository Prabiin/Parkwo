using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string? UserName,
    string? FullName,
    string? Email,
    bool? IsActive,
    Guid? RoleId)
    : IRequestResult<UpdateUserCommand, Unit>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.UserName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.FullName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Email)
            .MaximumLength(500)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .When(x => x.RoleId.HasValue);
    }
}

public sealed class UpdateUserCommandHandler(IApplicationDbContext context)
    : IRequestResultHandler<UpdateUserCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.BackOfficeUsers
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure("User not found.", 404);

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var normalizedUserName = request.UserName.Trim().ToLower();
            var existingUser = await context.BackOfficeUsers
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName && u.Id != request.UserId, cancellationToken);

            if (existingUser is not null)
                return Result<Unit>.Failure("Username already exists.", 400);

            user.UserName = normalizedUserName;
        }

        // Update full name if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var existingEmail = await context.BackOfficeUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != request.UserId, cancellationToken);

            if (existingEmail is not null)
                return Result<Unit>.Failure("Email already exists.", 400);

            user.Email = normalizedEmail;
        }

        // Update IsActive if provided
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        // Update role if provided (1:1)
        if (request.RoleId.HasValue)
        {
            // Validate role ID exists
            var roleExists = await context.BackOfficeRoles
                .AnyAsync(r => r.Id == request.RoleId.Value, cancellationToken);

            if (!roleExists)
                return Result<Unit>.Failure("Invalid role ID.", 400);

            if (user.UserRole is null)
            {
                user.UserRole = new BackOfficeUserRole
                {
                    RoleId = request.RoleId.Value,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
            }
            else
            {
                user.UserRole.RoleId = request.RoleId.Value;
            }
        }

        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Application.BackOffice.Commands.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string FullName,
    string Email,
    string Password,
    IReadOnlyList<Guid> RoleIds)
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.RoleIds)
            .NotNull();
    }
}

public sealed class CreateUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher)
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

        // Validate role IDs exist
        if (request.RoleIds.Count > 0)
        {
            var validRoleIds = await context.BackOfficeRoles
                .Where(r => request.RoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            var invalidIds = request.RoleIds.Except(validRoleIds).ToList();
            if (invalidIds.Count > 0)
                return Result<CreateUserResponse>.Failure($"Invalid role IDs: {string.Join(", ", invalidIds)}", 400);
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = new BackOfficeUser
        {
            UserName = normalizedUserName,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (request.RoleIds.Count > 0)
        {
            user.UserRoles = request.RoleIds
                .Select(id => new BackOfficeUserRole
                {
                    RoleId = id,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                })
                .ToList();
        }

        context.BackOfficeUsers.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateUserResponse>.Success(
            new CreateUserResponse(user.Id, user.UserName));
    }
}
using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Organizations.Commands.Create;

public sealed record CreateOrganizationCommand(
    string Name,
    string RegistrationNumber,
    string ContactNumber,
    string Address)
    : IRequestResult<CreateOrganizationCommand, Guid>;

public sealed class CreateOrganizationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateOrganizationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        var registrationNumber = request.RegistrationNumber.Trim();

        var exists = await context.Organizations
            .AnyAsync(o => o.RegistrationNumber == registrationNumber, cancellationToken);

        if (exists)
            return Result<Guid>.Failure(
                "An organization with this registration number already exists.", 409);

        var organization = new Organization
        {
            Name = request.Name.Trim(),
            RegistrationNumber = registrationNumber,
            ContactNumber = request.ContactNumber.Trim(),
            Address = request.Address.Trim(),
            OwnerUserId = userId.Value,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.Organizations.Add(organization);

        var membership = new UserOrganization
        {
            UserId = userId.Value,
            OrganizationId = organization.Id,
            Role = OrganizationRoleEnum.Owner,
            JoinedAtUtc = DateTimeOffset.UtcNow
        };

        context.UserOrganizations.Add(membership);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(organization.Id);
    }
}
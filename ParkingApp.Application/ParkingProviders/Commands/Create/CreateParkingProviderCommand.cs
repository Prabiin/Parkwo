using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Commands.Create;

public sealed record CreateParkingProviderCommand(
    string ProviderType,
    Guid? OrganizationId)
    : IRequestResult<CreateParkingProviderCommand, Guid>;

public sealed class CreateParkingProviderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestResultHandler<CreateParkingProviderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateParkingProviderCommand request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<Guid>.Failure("Authentication required.", 401);

        if (!Enum.TryParse<ProviderTypeEnum>(request.ProviderType, ignoreCase: true, out var providerType))
            return Result<Guid>.Failure("Invalid provider type.");

        var provider = new ParkingProvider
        {
            ProviderType = providerType,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (providerType == ProviderTypeEnum.Individual)
        {
            if (request.OrganizationId is not null)
                return Result<Guid>.Failure(
                    "OrganizationId is not allowed for an individual parking provider.");

            var alreadyIndividual = await context.ParkingProviders
                .AnyAsync(p => p.OwnerUserId == userId, cancellationToken);

            if (alreadyIndividual)
                return Result<Guid>.Failure(
                    "You already have an individual parking provider profile.", 409);

            provider.OwnerUserId = userId;
        }
        else
        {
            if (request.OrganizationId is null)
                return Result<Guid>.Failure(
                    "OrganizationId is required for a company parking provider.");

            var isOwner = await context.UserOrganizations
                .AnyAsync(m => m.UserId == userId
                               && m.OrganizationId == request.OrganizationId
                               && m.Role == OrganizationRoleEnum.Owner, cancellationToken);

            if (!isOwner)
                return Result<Guid>.Failure(
                    "You must own the organization to register it as a parking provider.", 403);

            var alreadyRegistered = await context.ParkingProviders
                .AnyAsync(p => p.OwnerOrganizationId == request.OrganizationId, cancellationToken);

            if (alreadyRegistered)
                return Result<Guid>.Failure(
                    "This organization already has a parking provider profile.", 409);

            provider.OwnerOrganizationId = request.OrganizationId;
        }

        context.ParkingProviders.Add(provider);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(provider.Id);
    }
}
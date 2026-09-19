using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Commands.Create;

public sealed class CreateParkingProviderCommandValidator : AbstractValidator<CreateParkingProviderCommand>
{
    public CreateParkingProviderCommandValidator()
    {
        RuleFor(x => x.ProviderType)
            .NotEmpty()
            .IsEnumName(typeof(ProviderTypeEnum));

        RuleFor(x => x.OrganizationId)
            .NotNull()
            .When(x => x.ProviderType.Equals("Company", StringComparison.OrdinalIgnoreCase))
            .WithMessage("OrganizationId is required for a company parking provider.");

        RuleFor(x => x.OrganizationId)
            .Null()
            .When(x => x.ProviderType.Equals("Individual", StringComparison.OrdinalIgnoreCase))
            .WithMessage("OrganizationId is not allowed for an individual parking provider.");
    }
}
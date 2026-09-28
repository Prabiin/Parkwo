using FluentValidation;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.ParkingProviders.Commands.Create;

public sealed class CreateParkingProviderCommandValidator : AbstractValidator<CreateParkingProviderCommand>
{
    public CreateParkingProviderCommandValidator()
    {
        RuleFor(x => x.ProviderType)
            .IsInEnum();

        RuleFor(x => x.OrganizationId)
            .NotNull()
            .When(x => x.ProviderType == ProviderTypeEnum.Company)
            .WithMessage("OrganizationId is required for a company parking provider.");

        RuleFor(x => x.OrganizationId)
            .Null()
            .When(x => x.ProviderType == ProviderTypeEnum.Individual)
            .WithMessage("OrganizationId is not allowed for an individual parking provider.");
    }
}

using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.BackOffice.Commands.Login;
using BackOfficeOrganizations = ParkingApp.Application.BackOffice.Queries.GetOrganizations;
using BackOfficeFacilities = ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;
using BackOfficeProviders = ParkingApp.Application.BackOffice.Queries.GetParkingProviders;
using ParkingApp.Application.BackOffice.Queries.GetRiders;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Facilities.Commands.Create;
using ParkingApp.Application.Facilities.Commands.CreateReview;
using ParkingApp.Application.Facilities.Commands.CreateSpots;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilities;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;
using ParkingApp.Application.Features.Logout.Command;
using ParkingApp.Application.Features.RefreshToken.Command;
using ParkingApp.Application.Features.SendOtp.Command;
using ParkingApp.Application.Features.VerifyOtp.Command;
using ParkingApp.Application.Organizations.Commands.Create;
using ParkingApp.Application.Organizations.Queries.GetOrganizations;
using ParkingApp.Application.ParkingProviders.Commands.Create;
using ParkingApp.Application.ParkingProviders.Queries.GetParkingProviders;
using ParkingApp.Application.Profile.Commands.Update;
using ParkingApp.Application.Profile.Queries.GetProfile;
using ParkingApp.Application.Vehicles.Commands.Create;
using ParkingApp.Application.Vehicles.Queries.GetVehicles;
using ParkingApp.Infrastructure.Auth;
using ParkingApp.Infrastructure.Cqrs;
using ParkingApp.Infrastructure.Files;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite());
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // JWT
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JWT settings are not configured.");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton(jwtSettings);

        // MinIO object storage (facility images)
        var minioSettings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>()
                            ?? throw new InvalidOperationException("MinIO settings are not configured.");

        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.AddSingleton(minioSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("BackOfficeOnly", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.Role, "BackOffice"));
        });

        services.AddHttpContextAccessor();

        // CQRS sender
        services.AddScoped<ISender, Sender>();

        // Request handlers (co-located with each command)
        services.AddScoped<IRequestResultHandler<SendOtpCommand, SendOtpResponse>, SendOtpCommandHandler>();
        services.AddScoped<IRequestResultHandler<VerifyOtpCommand, VerifyOtpResponse>, VerifyOtpCommandHandler>();
        services.AddScoped<IRequestResultHandler<RefreshTokenCommand, RefreshTokenResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<IRequestResultHandler<LogoutCommand, LogoutResponse>, LogoutCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreateVehicleCommand, Guid>, CreateVehicleCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateProfileCommand, Guid>, UpdateProfileCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetProfileQuery, GetProfileResponse>, GetProfileQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetVehiclesQuery, GetVehiclesResponse>, GetVehiclesQueryHandler>();
        services.AddScoped<IRequestResultHandler<BackOfficeLoginCommand, BackOfficeLoginResponse>, BackOfficeLoginCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetRidersQuery, GetRidersResponse>, GetRidersQueryHandler>();
        services.AddScoped<IRequestResultHandler<BackOfficeOrganizations.GetOrganizationsQuery, BackOfficeOrganizations.GetOrganizationsResponse>, BackOfficeOrganizations.GetOrganizationsQueryHandler>();
        services.AddScoped<IRequestResultHandler<BackOfficeProviders.GetParkingProvidersQuery, BackOfficeProviders.GetParkingProvidersResponse>, BackOfficeProviders.GetParkingProvidersQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateOrganizationCommand, Guid>, CreateOrganizationCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetOrganizationsQuery, GetOrganizationsResponse>, GetOrganizationsQueryHandler>();
        services.AddScoped<IRequestResultHandler<CreateParkingProviderCommand, Guid>, CreateParkingProviderCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingProvidersQuery, GetParkingProvidersResponse>, GetParkingProvidersQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateParkingFacilityCommand, Guid>, CreateParkingFacilityCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreateParkingSpotsCommand, Unit>, CreateParkingSpotsCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>, GetParkingFacilitiesQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityByIdQuery, GetParkingFacilityByIdResponse>, GetParkingFacilityByIdQueryHandler>();
        services.AddScoped<IRequestResultHandler<BackOfficeFacilities.GetParkingFacilitiesQuery, BackOfficeFacilities.GetParkingFacilitiesResponse>, BackOfficeFacilities.GetParkingFacilitiesQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>, GetParkingFacilityDetailQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateParkingFacilityReviewCommand, Guid>, CreateParkingFacilityReviewCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityReviewsQuery, GetParkingFacilityReviewsResponse>, GetParkingFacilityReviewsQueryHandler>();

        // Command validators
        services.AddScoped<IValidator<SendOtpCommand>, SendOtpCommandValidator>();
        services.AddScoped<IValidator<VerifyOtpCommand>, VerifyOtpCommandValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<IValidator<LogoutCommand>, LogoutCommandValidator>();
        services.AddScoped<IValidator<CreateVehicleCommand>, CreateVehicleCommandValidator>();
        services.AddScoped<IValidator<UpdateProfileCommand>, UpdateProfileCommandValidator>();
        services.AddScoped<IValidator<BackOfficeLoginCommand>, BackOfficeLoginCommandValidator>();
        services.AddScoped<IValidator<GetRidersQuery>, GetRidersQueryValidator>();
        services.AddScoped<IValidator<BackOfficeOrganizations.GetOrganizationsQuery>, BackOfficeOrganizations.GetOrganizationsQueryValidator>();
        services.AddScoped<IValidator<BackOfficeProviders.GetParkingProvidersQuery>, BackOfficeProviders.GetParkingProvidersQueryValidator>();
        services.AddScoped<IValidator<CreateOrganizationCommand>, CreateOrganizationCommandValidator>();
        services.AddScoped<IValidator<CreateParkingProviderCommand>, CreateParkingProviderCommandValidator>();
        services.AddScoped<IValidator<CreateParkingFacilityCommand>, CreateParkingFacilityCommandValidator>();
        services.AddScoped<IValidator<CreateParkingSpotsCommand>, CreateParkingSpotsCommandValidator>();
        services.AddScoped<IValidator<GetParkingFacilityByIdQuery>, GetParkingFacilityByIdQueryValidator>();
        services.AddScoped<IValidator<BackOfficeFacilities.GetParkingFacilitiesQuery>, BackOfficeFacilities.GetParkingFacilitiesQueryValidator>();
        services.AddScoped<IValidator<GetParkingFacilityDetailQuery>, GetParkingFacilityDetailQueryValidator>();
        services.AddScoped<IValidator<CreateParkingFacilityReviewCommand>, CreateParkingFacilityReviewCommandValidator>();
        services.AddScoped<IValidator<GetParkingFacilityReviewsQuery>, GetParkingFacilityReviewsQueryValidator>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBackOfficeTokenService, BackOfficeTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IOtpSender, ConsoleOtpSender>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
    }
}


using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ParkingApp.Application.Auth.Interfaces;
using ParkingApp.Application.BackOffice.Commands.Login;
using ParkingApp.Application.Bookings.Commands.Cancel;
using ParkingApp.Application.Bookings.Commands.Create;
using ParkingApp.Application.Bookings.Commands.RotatePass;
using ParkingApp.Application.Bookings.Commands.ScanGate;
using ParkingApp.Application.Bookings.Queries.GetBookingById;
using ParkingApp.Application.Bookings.Queries.GetBookingPass;
using ParkingApp.Application.Bookings.Queries.GetBookingSummary;
using ParkingApp.Application.Bookings.Queries.GetExitSummary;
using ParkingApp.Application.Bookings.Queries.GetOverstaySummary;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Application.Payments.Commands.CreateOverstay;
using ParkingApp.Application.Payments.Commands.ProcessCallback;
using ParkingApp.Application.Payments.Commands.ProcessOverstayCallback;
using ParkingApp.Application.Payments.Queries.GetPaymentGateways;
using ParkingApp.Application.Payments.Queries.GetBookingTransactions;
using ParkingApp.Application.BackOffice.Commands.UpdateApproval;
using BackOfficeOrganizations = ParkingApp.Application.BackOffice.Queries.GetOrganizations;
using BackOfficeFacilities = ParkingApp.Application.BackOffice.Queries.GetParkingFacilities;
using ParkingApp.Application.BackOffice.Queries.GetParkingFacilityDetail;
using BackOfficeProviders = ParkingApp.Application.BackOffice.Queries.GetParkingProviders;
using ParkingApp.Application.BackOffice.Queries.GetLicenses;
using ParkingApp.Application.BackOffice.Queries.GetRiders;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
using ParkingApp.Application.Facilities.Commands.Create;
using ParkingApp.Application.Facilities.Commands.CreateReview;
using ParkingApp.Application.Facilities.Commands.UpdateCapacity;
using ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;
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
using ParkingApp.Application.Licenses.Commands.Create;
using ParkingApp.Application.Licenses.Queries.GetDrivingLicense;
using ParkingApp.Infrastructure.Auth;
using ParkingApp.Infrastructure.Cqrs;
using ParkingApp.Infrastructure.Files;
using ParkingApp.Infrastructure.Payments;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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

        // OTP delivery. Optional section: ExposeDevCode lets local testing skip
        // the SMS gateway, but the code is withheld outside Development even if
        // the setting is turned on, so a stray Otp__ExposeDevCode=true env var
        // cannot leak real login codes from production. Only the singleton is
        // registered: an IOptions binding would hand out the raw configured
        // value and silently bypass the environment fail-safe above.
        var otpSettings = configuration.GetSection(OtpSettings.SectionName).Get<OtpSettings>()
                          ?? new OtpSettings();

        if (!environment.IsDevelopment())
        {
            otpSettings.ExposeDevCode = false;
        }

        services.AddSingleton(otpSettings);

        // RateLimitingSettings is bound here so command handlers can inject it,
        // but the limiter itself is registered by AddOtpRateLimiting in the web
        // project: AddRateLimiter needs the ASP.NET Core shared framework,
        // which this class library deliberately does not reference.
        var rateLimitingSettings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
                                   ?? new RateLimitingSettings();

        if (environment.IsDevelopment())
        {
            rateLimitingSettings.OtpSendPermitLimit = rateLimitingSettings.OtpSendDevPermitLimit;
            rateLimitingSettings.OtpVerifyPermitLimit = rateLimitingSettings.OtpVerifyDevPermitLimit;
            rateLimitingSettings.OtpSendPerPhoneLimit = rateLimitingSettings.OtpSendPerPhoneDevLimit;
        }

        services.AddSingleton(rateLimitingSettings);

        // MinIO object storage (facility images)
        var minioSettings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>()
                            ?? throw new InvalidOperationException("MinIO settings are not configured.");

        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.AddSingleton(minioSettings);

        // Upload limits (image extensions + max MB). Optional section:
        // code defaults apply when neither appsettings nor env provides it,
        // so the server runs before Upload__* env vars are set.
        var uploadSettings = configuration.GetSection(UploadSettings.SectionName).Get<UploadSettings>()
                             ?? new UploadSettings();

        services.Configure<UploadSettings>(configuration.GetSection(UploadSettings.SectionName));
        services.AddSingleton(uploadSettings);

        // Parking area standards for compliance plausibility checks. Optional
        // section with code defaults, overridable via ParkingStandards__* env.
        var parkingStandards = configuration.GetSection(ParkingStandards.SectionName).Get<ParkingStandards>()
                               ?? new ParkingStandards();

        services.Configure<ParkingStandards>(configuration.GetSection(ParkingStandards.SectionName));
        services.AddSingleton(parkingStandards);

        // Gate passes. Optional section, same posture as the gateway below: an
        // unset signing key leaves passes switched off instead of blocking the
        // whole boot. The service fails closed (nothing verifies, nothing signs)
        // and the pass handlers answer with a "not configured" message naming the
        // missing variable, so an environment missing a secret comes up with
        // every other feature working and one actionable diagnostic.
        var passSettings = configuration.GetSection(PassSettings.SectionName).Get<PassSettings>()
                           ?? new PassSettings();

        services.Configure<PassSettings>(configuration.GetSection(PassSettings.SectionName));
        services.AddSingleton(passSettings);
        services.AddSingleton<ParkingPassService>();

        // Khalti ePayment v2 (parking checkout). Optional section: payments stay
        // unavailable until Khalti__SecretKey is set, so the server boots and
        // every non-payment feature works without gateway credentials.
        var khaltiSection = configuration.GetSection(KhaltiSettings.SectionName);
        var khaltiSettings = khaltiSection.Get<KhaltiSettings>();

        if (khaltiSettings is { SecretKey: { } secretKey } &&
            !string.IsNullOrWhiteSpace(secretKey))
        {
            services.Configure<KhaltiSettings>(khaltiSection);
            services.AddSingleton(khaltiSettings);

            // Typed client: BaseAddress carries the API root (…/api/v2/) so the
            // adapter only names relative endpoints. Registered only when a
            // secret exists, so an unconfigured deployment has no gateway and
            // PaymentGateways.Find fails loudly instead of silently no-oping.
            services.AddHttpClient<IPaymentGateway, KhaltiPaymentGateway>(client =>
            {
                client.BaseAddress = new Uri(khaltiSettings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });
        }

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

            // Stops a phone-verified account that has not finished onboarding
            // from reaching any rider endpoint. Profile and auth stay reachable
            // so the account can actually complete registration.
            options.AddPolicy(ProfileCompleteRequirement.PolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProfileCompleteRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, ProfileCompleteAuthorizationHandler>();

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
        services.AddScoped<IRequestResultHandler<UpdateFacilityCapacityCommand, Unit>, UpdateFacilityCapacityCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateFacilityCapacityApprovalCommand, Unit>, UpdateFacilityCapacityApprovalCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilitiesQuery, GetParkingFacilitiesResponse>, GetParkingFacilitiesQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityByIdQuery, GetParkingFacilityByIdResponse>, GetParkingFacilityByIdQueryHandler>();
        services.AddScoped<IRequestResultHandler<BackOfficeFacilities.GetParkingFacilitiesQuery, BackOfficeFacilities.GetParkingFacilitiesResponse>, BackOfficeFacilities.GetParkingFacilitiesQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityDetailQuery, GetParkingFacilityDetailResponse>, GetParkingFacilityDetailQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetNearbyFacilitiesQuery, GetNearbyFacilitiesResponse>, GetNearbyFacilitiesQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateParkingFacilityReviewCommand, Guid>, CreateParkingFacilityReviewCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetParkingFacilityReviewsQuery, GetParkingFacilityReviewsResponse>, GetParkingFacilityReviewsQueryHandler>();
        services.AddScoped<IRequestResultHandler<UpdateOrganizationApprovalCommand, Unit>, UpdateOrganizationApprovalCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateParkingFacilityApprovalCommand, Unit>, UpdateParkingFacilityApprovalCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateDrivingLicenseApprovalCommand, Unit>, UpdateDrivingLicenseApprovalCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetLicensesQuery, GetLicensesResponse>, GetLicensesQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateDrivingLicenseCommand, Guid>, CreateDrivingLicenseCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetDrivingLicenseQuery, DrivingLicenseResponse?>, GetDrivingLicenseQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateBookingCommand, CreateBookingResponse>, CreateBookingCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetBookingByIdQuery, GetBookingByIdResponse>, GetBookingByIdQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetBookingSummaryQuery, GetBookingSummaryResponse>, GetBookingSummaryQueryHandler>();
        services.AddScoped<IRequestResultHandler<CancelBookingCommand, Unit>, CancelBookingCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreatePaymentCommand, CreatePaymentResponse>, CreatePaymentCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreateOverstayPaymentCommand, CreateOverstayPaymentResponse>, CreateOverstayPaymentCommandHandler>();
        services.AddScoped<IRequestResultHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResponse>, ProcessPaymentCallbackCommandHandler>();
        services.AddScoped<IRequestResultHandler<ProcessOverstayPaymentCallbackCommand, ProcessOverstayPaymentCallbackResponse>, ProcessOverstayPaymentCallbackCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetPaymentGatewaysQuery, GetPaymentGatewaysResponse>, GetPaymentGatewaysQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetBookingTransactionsQuery, GetBookingTransactionsResponse>, GetBookingTransactionsQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetBookingPassQuery, GetBookingPassResponse>, GetBookingPassQueryHandler>();
        services.AddScoped<IRequestResultHandler<RotatePassCommand, RotatePassResponse>, RotatePassCommandHandler>();
        services.AddScoped<IRequestResultHandler<ScanGateCommand, ScanGateResponse>, ScanGateCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetExitSummaryQuery, GetExitSummaryResponse>, GetExitSummaryQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetOverstaySummaryQuery, GetOverstaySummaryResponse>, GetOverstaySummaryQueryHandler>();

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
        services.AddScoped<IValidator<CreateOrganizationCommand>, CreateOrganizationCommandValidator>();
        services.AddScoped<IValidator<CreateParkingProviderCommand>, CreateParkingProviderCommandValidator>();
        services.AddScoped<IValidator<CreateParkingFacilityCommand>, CreateParkingFacilityCommandValidator>();
        services.AddScoped<IValidator<UpdateFacilityCapacityCommand>, UpdateFacilityCapacityCommandValidator>();
        services.AddScoped<IValidator<UpdateFacilityCapacityApprovalCommand>, UpdateFacilityCapacityApprovalCommandValidator>();
        services.AddScoped<IValidator<GetParkingFacilityByIdQuery>, GetParkingFacilityByIdQueryValidator>();
        services.AddScoped<IValidator<BackOfficeFacilities.GetParkingFacilitiesQuery>, BackOfficeFacilities.GetParkingFacilitiesQueryValidator>();
        services.AddScoped<IValidator<GetParkingFacilityDetailQuery>, GetParkingFacilityDetailQueryValidator>();
        services.AddScoped<IValidator<GetNearbyFacilitiesQuery>, GetNearbyFacilitiesQueryValidator>();
        services.AddScoped<IValidator<CreateParkingFacilityReviewCommand>, CreateParkingFacilityReviewCommandValidator>();
        services.AddScoped<IValidator<GetParkingFacilityReviewsQuery>, GetParkingFacilityReviewsQueryValidator>();
        services.AddScoped<IValidator<UpdateOrganizationApprovalCommand>, UpdateOrganizationApprovalCommandValidator>();
        services.AddScoped<IValidator<UpdateParkingFacilityApprovalCommand>, UpdateParkingFacilityApprovalCommandValidator>();
        services.AddScoped<IValidator<UpdateDrivingLicenseApprovalCommand>, UpdateDrivingLicenseApprovalCommandValidator>();
        services.AddScoped<IValidator<GetLicensesQuery>, GetLicensesQueryValidator>();
        services.AddScoped<IValidator<CreateDrivingLicenseCommand>, CreateDrivingLicenseCommandValidator>();
        services.AddScoped<IValidator<CreateBookingCommand>, CreateBookingCommandValidator>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBackOfficeTokenService, BackOfficeTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IOtpSender, ConsoleOtpSender>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
    }
}

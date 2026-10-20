
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
using ParkingApp.Application.Bookings.Commands.CreateWalkIn;
using ParkingApp.Application.Bookings.Commands.CompleteWalkIn;
using ParkingApp.Application.Bookings.Queries.GetBookingById;
using ParkingApp.Application.Bookings.Queries.GetBookingInit;
using ParkingApp.Application.Bookings.Queries.GetBookingPass;
using ParkingApp.Application.Bookings.Queries.GetBookingSummary;
using ParkingApp.Application.Bookings.Queries.GetExitSummary;
using ParkingApp.Application.Bookings.Queries.GetOverstaySummary;
using ParkingApp.Application.Bookings.Queries.GetWalkInPass;
using ParkingApp.Application.Bookings.Queries.GetWalkIns;
using ParkingApp.Application.Bookings.Queries.FindWalkInByPhone;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Application.Payments.Commands.CreateOverstay;
using ParkingApp.Application.Payments.Commands.CreateWalkIn;
using ParkingApp.Application.Payments.Commands.CreateWalkInOverstay;
using ParkingApp.Application.Payments.Commands.VerifyWalkIn;
using ParkingApp.Application.Payments.Commands.VerifyWalkInOverstay;
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
using ParkingApp.Application.Facilities.Queries.GetFacilityAvailability;
using ParkingApp.Application.Facilities.Queries.GetNearbyFacilities;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilities;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityById;
using ParkingApp.Application.Facilities.Queries.GetParkingFacilityReviews;
using ParkingApp.Application.Facilities.Queries.GetPublicFacilityDetail;
using ParkingApp.Application.FreeParkings.Commands.Create;
using ParkingApp.Application.FreeParkings.Commands.Delete;
using ParkingApp.Application.FreeParkings.Commands.Update;
using ParkingApp.Application.FreeParkings.Queries.GetFreeParkingById;
using ParkingApp.Application.FreeParkings.Queries.GetFreeParkings;
using ParkingApp.Application.FreeParkings.Queries.GetPublicFreeParking;
using ParkingApp.Application.StaffAuth.Commands.ChangePassword;
using ParkingApp.Application.StaffAuth.Commands.Login;
using ParkingApp.Application.StaffAuth.Commands.Logout;
using ParkingApp.Application.StaffAuth.Commands.RefreshToken;
using ParkingApp.Application.Staffs.Commands.Create;
using ParkingApp.Application.Staffs.Commands.Delete;
using ParkingApp.Application.Staffs.Commands.ResetPassword;
using ParkingApp.Application.Staffs.Commands.SetActive;
using ParkingApp.Application.Staffs.Commands.Update;
using ParkingApp.Application.Staffs.Queries.GetStaffById;
using ParkingApp.Application.Staffs.Queries.GetStaffInit;
using ParkingApp.Application.Staffs.Queries.GetStaffProfile;
using ParkingApp.Application.Staffs.Queries.GetStaffs;
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

        // Parkwo-owned rates. Providers never price their own spaces, so a
        // single global rate per vehicle type is read from config and snapshotted
        // onto each booking at create. Defaults apply when the section is absent.
        var parkwoPricing = configuration.GetSection(ParkwoPricingSettings.SectionName).Get<ParkwoPricingSettings>()
                           ?? new ParkwoPricingSettings();

        services.Configure<ParkwoPricingSettings>(configuration.GetSection(ParkwoPricingSettings.SectionName));
        services.AddSingleton(parkwoPricing);

        // Rider discovery. The "near me" radius is fixed here rather than a
        // client parameter — small on purpose, because a rider who wants a
        // different area searches for it by name. Defaults apply when the
        // section is absent; override with Nearby__NearMeRadiusKm.
        var nearbySettings = configuration.GetSection(NearbySettings.SectionName).Get<NearbySettings>()
                             ?? new NearbySettings();

        services.Configure<NearbySettings>(configuration.GetSection(NearbySettings.SectionName));
        services.AddSingleton(nearbySettings);

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

            // The staff realm. StaffOnly authenticates any staff token (login,
            // refresh, change-password, profile); StaffOperational additionally
            // holds back the gate and walk-in routes until the generated password
            // has been replaced.
            options.AddPolicy("StaffOnly", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.Role, StaffRoles.Staff));

            options.AddPolicy(StaffPasswordChangedRequirement.PolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimTypes.Role, StaffRoles.Staff);
                policy.AddRequirements(new StaffPasswordChangedRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, ProfileCompleteAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, StaffPasswordChangedAuthorizationHandler>();

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
        services.AddScoped<IRequestResultHandler<GetFacilityAvailabilityQuery, GetFacilityAvailabilityResponse>, GetFacilityAvailabilityQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetPublicFacilityDetailQuery, GetPublicFacilityDetailResponse>, GetPublicFacilityDetailQueryHandler>();

        services.AddScoped<IRequestResultHandler<CreateFreeParkingCommand, Guid>, CreateFreeParkingCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateFreeParkingCommand, Unit>, UpdateFreeParkingCommandHandler>();
        services.AddScoped<IRequestResultHandler<DeleteFreeParkingCommand, Unit>, DeleteFreeParkingCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetFreeParkingsQuery, GetFreeParkingsResponse>, GetFreeParkingsQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetFreeParkingByIdQuery, GetFreeParkingByIdResponse>, GetFreeParkingByIdQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetPublicFreeParkingQuery, GetPublicFreeParkingResponse>, GetPublicFreeParkingQueryHandler>();

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
        services.AddScoped<IRequestResultHandler<GetBookingInitQuery, GetBookingInitResponse>, GetBookingInitQueryHandler>();
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

        // Walk-in desk: staff create, pay, verify and close bookings for riders
        // with no Parkwo account. The gate scan itself is shared with app riders.
        services.AddScoped<IRequestResultHandler<CreateWalkInBookingCommand, CreateWalkInBookingResponse>, CreateWalkInBookingCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreateWalkInPaymentCommand, CreatePaymentResponse>, CreateWalkInPaymentCommandHandler>();
        services.AddScoped<IRequestResultHandler<VerifyWalkInPaymentCommand, VerifyWalkInPaymentResponse>, VerifyWalkInPaymentCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetWalkInPassQuery, GetBookingPassResponse>, GetWalkInPassQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetWalkInsQuery, GetWalkInsResponse>, GetWalkInsQueryHandler>();
        services.AddScoped<IRequestResultHandler<FindWalkInByPhoneQuery, FindWalkInByPhoneResponse>, FindWalkInByPhoneQueryHandler>();
        services.AddScoped<IRequestResultHandler<CompleteWalkInExitCommand, CompleteWalkInExitResponse>, CompleteWalkInExitCommandHandler>();
        services.AddScoped<IRequestResultHandler<CreateWalkInOverstayPaymentCommand, CreateOverstayPaymentResponse>, CreateWalkInOverstayPaymentCommandHandler>();
        services.AddScoped<IRequestResultHandler<VerifyWalkInOverstayPaymentCommand, VerifyWalkInOverstayPaymentResponse>, VerifyWalkInOverstayPaymentCommandHandler>();

        // Staff: BackOffice CRUD plus the staff app's own auth and profile.
        services.AddScoped<IRequestResultHandler<CreateStaffCommand, CreateStaffResponse>, CreateStaffCommandHandler>();
        services.AddScoped<IRequestResultHandler<UpdateStaffCommand, Unit>, UpdateStaffCommandHandler>();
        services.AddScoped<IRequestResultHandler<ResetStaffPasswordCommand, Unit>, ResetStaffPasswordCommandHandler>();
        services.AddScoped<IRequestResultHandler<SetStaffActiveCommand, Unit>, SetStaffActiveCommandHandler>();
        services.AddScoped<IRequestResultHandler<DeleteStaffCommand, Unit>, DeleteStaffCommandHandler>();
        services.AddScoped<IRequestResultHandler<GetStaffsQuery, GetStaffsResponse>, GetStaffsQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetStaffByIdQuery, GetStaffByIdResponse>, GetStaffByIdQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetStaffInitQuery, GetStaffInitResponse>, GetStaffInitQueryHandler>();
        services.AddScoped<IRequestResultHandler<GetStaffProfileQuery, GetStaffProfileResponse>, GetStaffProfileQueryHandler>();
        services.AddScoped<IRequestResultHandler<StaffLoginCommand, StaffLoginResponse>, StaffLoginCommandHandler>();
        services.AddScoped<IRequestResultHandler<StaffRefreshTokenCommand, StaffRefreshTokenResponse>, StaffRefreshTokenCommandHandler>();
        services.AddScoped<IRequestResultHandler<StaffChangePasswordCommand, Unit>, StaffChangePasswordCommandHandler>();
        services.AddScoped<IRequestResultHandler<StaffLogoutCommand, Unit>, StaffLogoutCommandHandler>();

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
        services.AddScoped<IValidator<GetFacilityAvailabilityQuery>, GetFacilityAvailabilityQueryValidator>();
        services.AddScoped<IValidator<GetPublicFacilityDetailQuery>, GetPublicFacilityDetailQueryValidator>();
        services.AddScoped<IValidator<GetBookingInitQuery>, GetBookingInitQueryValidator>();
        services.AddScoped<IValidator<CreateFreeParkingCommand>, CreateFreeParkingCommandValidator>();
        services.AddScoped<IValidator<UpdateFreeParkingCommand>, UpdateFreeParkingCommandValidator>();
        services.AddScoped<IValidator<CreateParkingFacilityReviewCommand>, CreateParkingFacilityReviewCommandValidator>();
        services.AddScoped<IValidator<GetParkingFacilityReviewsQuery>, GetParkingFacilityReviewsQueryValidator>();
        services.AddScoped<IValidator<UpdateOrganizationApprovalCommand>, UpdateOrganizationApprovalCommandValidator>();
        services.AddScoped<IValidator<UpdateParkingFacilityApprovalCommand>, UpdateParkingFacilityApprovalCommandValidator>();
        services.AddScoped<IValidator<UpdateDrivingLicenseApprovalCommand>, UpdateDrivingLicenseApprovalCommandValidator>();
        services.AddScoped<IValidator<GetLicensesQuery>, GetLicensesQueryValidator>();
        services.AddScoped<IValidator<CreateDrivingLicenseCommand>, CreateDrivingLicenseCommandValidator>();
        services.AddScoped<IValidator<CreateBookingCommand>, CreateBookingCommandValidator>();
        services.AddScoped<IValidator<CreateWalkInBookingCommand>, CreateWalkInBookingCommandValidator>();
        services.AddScoped<IValidator<CreateStaffCommand>, CreateStaffCommandValidator>();
        services.AddScoped<IValidator<UpdateStaffCommand>, UpdateStaffCommandValidator>();
        services.AddScoped<IValidator<StaffLoginCommand>, StaffLoginCommandValidator>();
        services.AddScoped<IValidator<StaffRefreshTokenCommand>, StaffRefreshTokenCommandValidator>();
        services.AddScoped<IValidator<StaffChangePasswordCommand>, StaffChangePasswordCommandValidator>();
        services.AddScoped<IValidator<StaffLogoutCommand>, StaffLogoutCommandValidator>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBackOfficeTokenService, BackOfficeTokenService>();
        services.AddScoped<IStaffTokenService, StaffTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IOtpSender, ConsoleOtpSender>();

        // Email delivery, config-gated the way Khalti is: with Email:Host and
        // Email:From set the deployment sends real SMTP mail; with neither it
        // falls back to the console sender, which only logs the message. The
        // half-configured case is a mistake rather than a fallback and is
        // rejected at startup — otherwise a server that had only been handed
        // Email__Host would drop generated passwords back into the logs while
        // looking configured, which is the silent failure this gate exists to
        // make impossible.
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()
                            ?? new EmailSettings();

        var emailHasHost = !string.IsNullOrWhiteSpace(emailSettings.Host);
        var emailHasFrom = !string.IsNullOrWhiteSpace(emailSettings.From);

        if (emailHasHost != emailHasFrom)
        {
            throw new InvalidOperationException(
                "Email settings are incomplete: set both Email:Host and Email:From to deliver mail, " +
                "or neither to fall back to the console sender.");
        }

        if (emailHasHost)
        {
            services.AddSingleton(emailSettings);
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IImageUrlResolver, MinioImageUrlResolver>();
    }
}

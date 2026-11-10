
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
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Application.Configuration;
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
            // BackOfficeOnly is the full console policy: a BackOffice token
            // whose user has already replaced the generated password.
            options.AddPolicy("BackOfficeOnly", policy =>
            {
                policy.RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.Role, "BackOffice");
                policy.AddRequirements(new BackOfficePasswordChangedRequirement());
            });

            // The narrow admin policy for login-adjacent routes: any live
            // BackOffice token, including one still on its generated password.
            options.AddPolicy("BackOfficeAuthenticated", policy =>
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
        services.AddScoped<IAuthorizationHandler, BackOfficePasswordChangedAuthorizationHandler>();

        services.AddHttpContextAccessor();

        // CQRS sender
        services.AddScoped<ISender, Sender>();

        // Request handlers and validators are discovered from the Application
        // assembly. Every concrete type implementing IRequestResultHandler<,>
        // or IValidator<> gets one scoped registration per closed interface, so
        // a new command/query needs no edit here. DependencyInjectionTests
        // guards that the scan picks every handler and validator up.
        AddApplicationHandlersAndValidators(services);

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBackOfficeTokenService, BackOfficeTokenService>();
        services.AddScoped<IStaffTokenService, StaffTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IOtpSender, ConsoleOtpSender>();

        // Email delivery, config-gated the way Khalti is. Email:Provider picks
        // the transport — "Resend" for the HTTP API, "Smtp" for a relay — and
        // with none set the console sender only logs the message. The
        // half-configured cases are mistakes rather than fallbacks and are
        // rejected at startup: a server handed an API key but no provider, or a
        // provider but no From, would either drop generated passwords back into
        // the logs while looking configured, or fail every send at the first
        // request. Both are the silent failure this gate exists to make
        // impossible, and both are cheap to catch here.
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()
                            ?? new EmailSettings();

        var provider = emailSettings.Provider.Trim();

        // No provider but SMTP fields set is the pre-existing shape: the
        // original gate keyed off Email:Host alone, so treating a bare Host as
        // "Smtp" keeps an already-deployed configuration working rather than
        // silently demoting it to console logs.
        if (provider.Length == 0 && !string.IsNullOrWhiteSpace(emailSettings.Host))
        {
            provider = EmailProvider.Smtp;
        }

        if (provider.Length == 0)
        {
            // A From address with no provider is a half-finished setup, not a
            // deployment that deliberately chose logging.
            if (!string.IsNullOrWhiteSpace(emailSettings.From))
            {
                throw new InvalidOperationException(
                    "Email settings are incomplete: Email:From is set but Email:Provider is not. " +
                    "Set Email:Provider to 'Resend' or 'Smtp', or clear the Email section to use the console sender.");
            }

            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }
        else if (EmailProvider.IsResend(provider))
        {
            RequireSetting(emailSettings.From, "Email:From");
            RequireSetting(emailSettings.ApiKey, "Email:ApiKey");

            services.AddSingleton(emailSettings);
            services.AddScoped<IEmailSender, ResendEmailSender>();
        }
        else if (EmailProvider.IsSmtp(provider))
        {
            RequireSetting(emailSettings.From, "Email:From");
            RequireSetting(emailSettings.Host, "Email:Host");

            services.AddSingleton(emailSettings);
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Email:Provider '{provider}' is not recognised. Use 'Resend' or 'Smtp', " +
                "or clear the Email section to fall back to the console sender.");
        }

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IImageUrlResolver, MinioImageUrlResolver>();
    }

    /// <summary>
    /// Rejects a blank setting that the chosen provider cannot work without.
    /// Only reached once a provider is selected, so it is the difference
    /// between a clear startup error and a first send that fails with
    /// something unhelpful — or, for From, a message rejected by the provider
    /// after the staff row was already created.
    /// </summary>
    private static void RequireSetting(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Email settings are incomplete: {name} is required for the configured provider.");
    }

    /// <summary>
    /// Registers every CQRS handler and FluentValidation validator found in the
    /// Application assembly. Handlers and validators implement a distinct closed
    /// generic interface per request/command, so each implemented interface is
    /// registered individually against its implementation type. Scanning keeps
    /// the DI wiring in sync automatically; <c>DependencyInjectionTests</c> still
    /// asserts that nothing was missed.
    /// </summary>
    private static void AddApplicationHandlersAndValidators(IServiceCollection services)
    {
        var applicationAssembly = typeof(IRequestResultHandler<,>).Assembly;

        foreach (var implementationType in applicationAssembly.GetTypes())
        {
            if (implementationType is not
                { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                continue;

            foreach (var serviceType in implementationType.GetInterfaces())
            {
                if (!IsHandlerOrValidatorInterface(serviceType))
                    continue;

                services.AddScoped(serviceType, implementationType);
            }
        }
    }

    private static bool IsHandlerOrValidatorInterface(Type type)
        => type.IsGenericType
           && (type.GetGenericTypeDefinition() == typeof(IRequestResultHandler<,>)
               || type.GetGenericTypeDefinition() == typeof(IValidator<>));
}

/// <summary>
/// The recognised <see cref="EmailSettings.Provider"/> values, compared
/// case-insensitively so an operator writing <c>resend</c> is not punished for
/// it. Kept beside the gate so the accepted names live in one place.
/// </summary>
internal static class EmailProvider
{
    public const string Resend = "Resend";
    public const string Smtp = "Smtp";

    public static bool IsResend(string provider)
        => string.Equals(provider, Resend, StringComparison.OrdinalIgnoreCase);

    public static bool IsSmtp(string provider)
        => string.Equals(provider, Smtp, StringComparison.OrdinalIgnoreCase);
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Api.Infrastructure.RateLimiting;

/// <summary>
/// Throttling for the OTP endpoints. Lives in the web project rather than
/// Infrastructure because AddRateLimiter needs the ASP.NET Core shared
/// framework, and Infrastructure is a plain class library.
///
/// The limiter is registered in every environment on purpose: a limiter that
/// only exists in production is never exercised locally, so policy-name and
/// middleware-ordering mistakes stay hidden until they matter. Development
/// simply gets the higher "Dev" limits, applied to the singleton by
/// AddInfrastructure before this runs.
///
/// Configuration goes through Configure&lt;RateLimitingSettings&gt; rather than
/// an AddRateLimiter lambda so it consumes the exact settings instance DI
/// resolved. Re-reading the configuration here would silently ignore the
/// Development overrides.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddOtpRateLimiting(this IServiceCollection services)
    {
        // Registers the middleware's dependencies (policy provider/cache, metrics).
        // The options themselves are configured below so RateLimitingSettings can
        // be injected rather than re-read from configuration.
        services.AddRateLimiter();

        services.AddOptions<RateLimiterOptions>()
            .Configure<RateLimitingSettings>((options, settings) =>
            {
                if (!settings.Enabled)
                    return;

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { Errors = new[] { "Too many requests. Please try again later." } },
                        cancellationToken);
                };

                options.AddPolicy(AuthRateLimitPolicies.OtpSend, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.OtpSendPermitLimit,
                            Window = TimeSpan.FromMinutes(settings.OtpSendWindowMinutes),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy(AuthRateLimitPolicies.OtpVerify, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.OtpVerifyPermitLimit,
                            Window = TimeSpan.FromMinutes(settings.OtpVerifyWindowMinutes),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
            });

        return services;
    }

    /// <summary>
    /// UseForwardedHeaders has already run by the time this executes, so
    /// RemoteIpAddress reflects the real client when behind a proxy.
    /// </summary>
    private static string ClientPartitionKey(HttpContext httpContext)
        => httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
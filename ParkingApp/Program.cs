using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ParkingApp.Api.Infrastructure;
using ParkingApp.Api.Infrastructure.RateLimiting;
using ParkingApp.Infrastructure;
using ParkingApp.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Logging: console always; Elasticsearch when configured (compose sets Elastic__Url,
// local runs use appsettings.Development.json). Absent ES, console alone — never fatal.
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ParkingApp.Api")
    .WriteTo.Console();

var elasticUrl = builder.Configuration["Elastic:Url"];
if (!string.IsNullOrWhiteSpace(elasticUrl))
{
    loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        IndexFormat = "parkingapp-logs-{0:yyyy.MM}",
        AutoRegisterTemplate = true
    });
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Rate limiting reads the RateLimitingSettings singleton that AddInfrastructure
// already bound and adjusted for the current environment.
builder.Services.AddOtpRateLimiting();

// Enums bind as numbers only (e.g. "gender": 1). String names ("Male") are
// rejected at deserialization (400); undefined ints (99) are caught by
// FluentValidation IsInEnum() in each command validator.
builder.Services.AddOpenApi();

// Swagger UI, for the mobile team (it is the UI they already know). Swashbuckle
// builds its OWN document, served at /swagger/v1/swagger.json — it does not read
// the AddOpenApi document above, so Scalar (/scalar) and Swagger UI (/swagger)
// stay independent and neither can break the other.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // AddOpenApi infers the bearer scheme from the ASP.NET Core auth stack;
    // Swashbuckle does not, so declare it by hand. Without this, Swagger UI
    // renders no "Authorize" button and try-it cannot send the accessToken
    // that POST /auth/verify-otp returns.
    const string bearerScheme = "Bearer";
    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken from the verify-otp response (no 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(bearerScheme, document)] = []
    });
});

var app = builder.Build();

// Behind Render (or any TLS-terminating proxy) the app receives plain HTTP
// plus X-Forwarded-Proto/For headers. Honor them so generated URLs (OpenAPI
// server, Scalar Try-it) use the public https scheme. Must run first.
// KnownNetworks/Proxies are cleared because cloud proxy IPs are dynamic;
// Render only routes to this app through its own proxy.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ParkingApp.Api v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ParkingApp API";
        options.EnableTryItOutByDefault();
        options.DisplayRequestDuration();
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

// Must sit after routing so endpoint-specific policies (RequireRateLimiting)
// can resolve their named policy.
app.UseRateLimiter();

app.MapEndpoints();

// Test-server convenience: apply pending EF migrations at startup
// (compose brings up postgres first via healthcheck, so this is safe here).
//
// Wrapped so a database hiccup degrades one feature instead of taking the whole
// API down. The endpoints that need these tables will 500 on their own, but auth,
// facilities and the rest stay reachable — a dead process helps nobody diagnose
// why it is dead.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Startup database migration failed; the API is running with an outdated schema.");
}

app.Run();
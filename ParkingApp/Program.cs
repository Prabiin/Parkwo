using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Api.Infrastructure;
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
builder.Services.AddInfrastructure(builder.Configuration);

// Allow enum names ("TwoWheeler", "Male") in JSON bodies, not just ints.
// Integer values still bind too. Needed now that commands take enums directly
// (e.g. CreateVehicleCommand.VehicleType, UpdateProfileCommand.Gender).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

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
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

// Test-server convenience: apply pending EF migrations at startup
// (compose brings up postgres first via healthcheck, so this is safe here).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
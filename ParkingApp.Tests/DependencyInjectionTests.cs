using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Infrastructure;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Tests;

/// <summary>
/// Guards on <c>AddInfrastructure</c>. Handlers and validators are wired up by
/// hand there, so adding a new query or command without its registration
/// compiles and the unit tests stay green — the app still boots — while the
/// endpoint dies at runtime with "No service for type
/// 'IRequestResultHandler`2[...]' has been registered" (a 500). The same file
/// picks infrastructure implementations by configuration, so the selection is
/// locked here too rather than only inspected by eye. If a test fails, add or
/// fix the registration in <c>DependencyInjection</c>.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void Every_handler_and_validator_in_the_application_is_registered()
    {
        var services = BuildServices();

        var registered = services.Select(d => d.ServiceType).ToHashSet();

        var missing = typeof(IRequestResultHandler<,>).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces())
            .Where(IsHandlerOrValidator)
            .Where(type => !registered.Contains(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void A_configured_email_section_selects_the_smtp_sender()
    {
        var services = BuildServices(new Dictionary<string, string?>
        {
            ["Email:Host"] = "smtp.example.com",
            ["Email:From"] = "noreply@example.com"
        });

        // Resolved rather than only inspected: this also proves the settings
        // instance was registered alongside it, which is a second registration
        // that can be forgotten exactly like a missing handler was.
        using var provider = services.BuildServiceProvider();

        Assert.IsType<SmtpEmailSender>(provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void No_email_configuration_falls_back_to_the_console_sender()
    {
        var services = BuildServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ConsoleEmailSender>(provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Half_configured_email_settings_fail_at_startup()
    {
        // A host without a from-address would otherwise register the SMTP
        // sender while half its required settings were missing — or worse,
        // silently fall back to logging the passwords it was meant to email.
        var exception = Assert.Throws<InvalidOperationException>(
            () => BuildServices(new Dictionary<string, string?>
            {
                ["Email:Host"] = "smtp.example.com"
            }));

        Assert.Contains("Email:Host", exception.Message);
    }

    private static ServiceCollection BuildServices(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=parkingapp;Username=test;Password=test",
            ["Jwt:Secret"] = "test-secret-test-secret-test-secret-test-secret",
            ["Jwt:Issuer"] = "ParkingApp.Tests",
            ["Jwt:Audience"] = "ParkingApp.Tests",
            ["Minio:Endpoint"] = "localhost:9000",
            ["Minio:AccessKey"] = "test",
            ["Minio:SecretKey"] = "test",
            ["Minio:PublicBaseUrl"] = "http://localhost:9000/parkingapp"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        // The host (WebApplicationBuilder) provides logging; AddInfrastructure
        // does not, so the test host has to, or nothing that takes ILogger<T>
        // could be resolved here.
        services.AddLogging();
        services.AddInfrastructure(configuration, new TestHostEnvironment());

        return services;
    }

    private static bool IsHandlerOrValidator(Type type)
        => type.IsGenericType
           && (type.GetGenericTypeDefinition() == typeof(IRequestResultHandler<,>)
               || type.GetGenericTypeDefinition() == typeof(IValidator<>));

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ParkingApp.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

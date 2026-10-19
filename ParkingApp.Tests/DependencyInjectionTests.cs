using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Infrastructure;

namespace ParkingApp.Tests;

/// <summary>
/// Handlers and validators are wired up by hand in <c>AddInfrastructure</c>.
/// Everything compiles and the unit tests stay green when a new query or command
/// is added without its registration — the app still boots — but the endpoint
/// then dies at runtime with "No service for type
/// 'IRequestResultHandler`2[...]' has been registered" (a 500). This locks the
/// wiring: every handler and validator in the Application assembly must have a
/// registration. If this test fails, add the missing <c>services.AddScoped</c>
/// line in <c>DependencyInjection</c>.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void Every_handler_and_validator_in_the_application_is_registered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
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
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration, new TestHostEnvironment());

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

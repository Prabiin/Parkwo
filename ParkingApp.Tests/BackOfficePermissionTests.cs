using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ParkingApp.Infrastructure;
using ParkingApp.Infrastructure.Auth;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Tests;

/// <summary>
/// The BackOffice permission check: an endpoint declares a menu key and an
/// action key (the same pair the role form stores), and the handler resolves
/// the caller's role live so a grant takes effect without a new login. These
/// pin the parts that fail silently — a missing handler registration turns
/// every protected endpoint into a 500, and a mis-shaped requirement would
/// match nothing.
/// </summary>
public class BackOfficePermissionTests
{
    [Fact]
    public void A_requirement_carries_the_menu_and_the_actions_it_demands()
    {
        var requirement = new BackOfficePermissionRequirement("User", ["List", "View"]);

        Assert.Equal("User", requirement.MenuKey);
        Assert.Equal(["List", "View"], requirement.Actions);
        // Any one of the listed actions grants access unless told otherwise.
        Assert.False(requirement.RequireAll);
    }

    [Fact]
    public void A_requirement_can_demand_every_action()
    {
        var requirement = new BackOfficePermissionRequirement("Role", ["Edit", "Delete"], requireAll: true);

        Assert.True(requirement.RequireAll);
    }

    [Fact]
    public void The_permission_handler_is_registered()
    {
        // A missing registration is not a compile error: the endpoint simply
        // fails at runtime with "No service for type IAuthorizationHandler".
        using var provider = BuildServiceProvider();

        var handlers = provider.GetServices<IAuthorizationHandler>();

        Assert.Contains(handlers, h => h is BackOfficePermissionAuthorizationHandler);
    }

    [Fact]
    public void The_permission_queries_translate_to_sql()
    {
        // A query EF Core cannot translate only fails at runtime, as a 500 on
        // every protected endpoint. ToQueryString builds the SQL without
        // touching a database, so the shape is proved here instead.
        using var context = BuildDbContext();

        var roleSql = BackOfficePermissionQueries
            .RoleNames(context, Guid.NewGuid())
            .ToQueryString();

        var actionSql = BackOfficePermissionQueries
            .GrantedActions(context, Guid.NewGuid(), "User")
            .ToQueryString();

        Assert.Contains("BackOfficeRoles", roleSql);
        Assert.Contains("BackOfficeUserRoles", roleSql);

        Assert.Contains("BackOfficeRoleMenuActions", actionSql);
        Assert.Contains("BackOfficeMenuActions", actionSql);
        // The menu key is a parameter, not baked into the SQL text.
        Assert.Contains("@menuKey", actionSql);
    }

    private static ApplicationDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=parkingapp;Username=test;Password=test",
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ServiceProvider BuildServiceProvider()
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
        services.AddLogging();
        services.AddInfrastructure(configuration, new TestHostEnvironment());

        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ParkingApp.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

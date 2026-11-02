using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingApp.Infrastructure.Persistence;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Api.Services;

public class SuperAdminPermissionSync : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SuperAdminPermissionSync> _logger;

    public SuperAdminPermissionSync(IServiceScopeFactory scopeFactory, ILogger<SuperAdminPermissionSync> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var superAdminRole = await db.BackOfficeRoles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin", ct);

            if (superAdminRole == null)
            {
                _logger.LogInformation("SuperAdmin role not found, skipping permission sync (tables may not be seeded yet)");
                return; // SuperAdmin role not seeded yet
            }

            var allActionIds = await db.BackOfficeMenuActions
                .Select(a => a.Id)
                .ToListAsync(ct);

            var existing = await db.BackOfficeRoleMenuActions
                .Where(x => x.RoleId == superAdminRole.Id)
                .Select(x => x.MenuActionId)
                .ToListAsync(ct);

            var missing = allActionIds.Except(existing).ToList();
            if (missing.Count > 0)
            {
                _logger.LogInformation("Assigning {Count} missing permissions to SuperAdmin role", missing.Count);
                db.BackOfficeRoleMenuActions.AddRange(missing.Select(maId => new BackOfficeRoleMenuAction
                {
                    RoleId = superAdminRole.Id,
                    MenuActionId = maId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                }));
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException || ex is Npgsql.PostgresException || ex is InvalidOperationException)
        {
            // Tables don't exist yet (migration not applied) - log and continue
            // This allows the app to start even if migrations haven't run yet
            _logger.LogWarning(ex, "SuperAdminPermissionSync skipped - BackOffice tables may not exist yet. Run migrations to enable permission sync.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SuperAdminPermissionSync");
            // Don't rethrow - allow app to start
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
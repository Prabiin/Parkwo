using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ParkingApp.Infrastructure.Persistence;
using ParkingApp.Domain.BackOffice.Role;

namespace ParkingApp.Api.Services;

public class SuperAdminPermissionSync : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SuperAdminPermissionSync(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var superAdminRole = await db.BackOfficeRoles
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin", ct);

        if (superAdminRole == null)
        {
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
            db.BackOfficeRoleMenuActions.AddRange(missing.Select(maId => new BackOfficeRoleMenuAction
            {
                RoleId = superAdminRole.Id,
                MenuActionId = maId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));
            await db.SaveChangesAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
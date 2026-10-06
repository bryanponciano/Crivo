using Crive.Api.Infrastructure;
using Crive.Api.Models;
using Crive.Shared.DTOs;
using Crive.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Services;

public class MachineService(AppDbContext dbContext)
{
    public async Task UpdateHeartbeatAsync(Guid machineId, ConnectionStatus status, string? activeRuleHash = null)
    {
        var machine = await dbContext.Machines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == machineId);

        if (machine != null)
        {
            machine.LastHeartbeat = DateTime.UtcNow;
            machine.ConnectionStatus = status;
            if (activeRuleHash != null)
            {
                machine.ActiveRuleHash = activeRuleHash;
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RecordBlockEventAsync(Guid machineId, BlockEventDto dto)
    {
        var machine = await dbContext.Machines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == machineId);

        if (machine != null)
        {
            var evt = new BlockEvent
            {
                TenantId = machine.TenantId,
                MachineId = machineId,
                RuleId = dto.RuleId,
                BlockedDomain = dto.BlockedDomain,
                BlockedAt = dto.BlockedAt,
                ProcessName = dto.ProcessName ?? string.Empty
            };
            dbContext.BlockEvents.Add(evt);
            await dbContext.SaveChangesAsync();
        }
    }

    // In a real app, this would run in a background hosted service
    public async Task CheckStaleConnectionsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var staleMachines = await dbContext.Machines
            .IgnoreQueryFilters()
            .Where(m => m.ConnectionStatus == ConnectionStatus.Online && m.LastHeartbeat < cutoff)
            .ToListAsync();

        foreach (var m in staleMachines)
        {
            m.ConnectionStatus = ConnectionStatus.Offline;
        }

        if (staleMachines.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }
    }
}

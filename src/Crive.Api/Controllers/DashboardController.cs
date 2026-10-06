using Crive.Api.Infrastructure;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalMachines = await dbContext.Machines.CountAsync();
        var onlineMachines = await dbContext.Machines.CountAsync(m => m.ConnectionStatus == Crive.Shared.Enums.ConnectionStatus.Online);
        var totalBlocks = await dbContext.BlockEvents.CountAsync();
        var activeRules = await dbContext.Rules.CountAsync(r => r.IsActive);
        var totalSectors = await dbContext.Sectors.CountAsync(s => s.IsActive);
        
        var recentBlocks = await dbContext.BlockEvents
            .Include(b => b.Machine)
            .Include(b => b.Rule)
            .OrderByDescending(b => b.BlockedAt)
            .Take(10)
            .Select(b => new BlockEventViewDto
            {
                MachineName = b.Machine.Hostname ?? b.Machine.AssetNumber,
                EmployeeName = b.Machine.EmployeeName,
                RuleName = b.Rule != null ? b.Rule.Name : "N/A",
                BlockedDomain = b.BlockedDomain,
                BlockedAt = b.BlockedAt,
                ProcessName = b.ProcessName
            })
            .ToListAsync();

        return Ok(new DashboardSummaryDto
        {
            TotalMachines = totalMachines,
            OnlineMachines = onlineMachines,
            OfflineMachines = totalMachines - onlineMachines,
            TotalBlocksToday = totalBlocks,
            ActiveRules = activeRules,
            TotalSectors = totalSectors,
            RecentBlocks = recentBlocks
        });
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var blocks = await dbContext.BlockEvents
            .Include(b => b.Machine)
            .Include(b => b.Rule)
            .OrderByDescending(b => b.BlockedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlockEventViewDto
            {
                MachineName = b.Machine.Hostname ?? b.Machine.AssetNumber,
                EmployeeName = b.Machine.EmployeeName,
                RuleName = b.Rule != null ? b.Rule.Name : "N/A",
                BlockedDomain = b.BlockedDomain,
                BlockedAt = b.BlockedAt,
                ProcessName = b.ProcessName
            })
            .ToListAsync();

        return Ok(blocks);
    }
}

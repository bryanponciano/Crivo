using Crive.Api.Infrastructure;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MachinesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMachines([FromQuery] Guid? sectorId)
    {
        var query = dbContext.Machines.AsQueryable();
        if (sectorId.HasValue)
            query = query.Where(m => m.SectorId == sectorId.Value);

        var machines = await query.Include(m => m.Sector).ToListAsync();
        
        var dtos = machines.Select(m => new MachineDto
        {
            Id = m.Id,
            SectorId = m.SectorId ?? Guid.Empty,
            SectorName = m.Sector?.Name ?? string.Empty,
            AssetNumber = m.AssetNumber,
            EmployeeName = m.EmployeeName,
            Hostname = m.Hostname ?? string.Empty,
            AgentVersion = m.AgentVersion ?? string.Empty,
            Status = m.ConnectionStatus,
            LastHeartbeat = m.LastHeartbeat,
            RegisteredAt = m.RegisteredAt
        });
        
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMachine(Guid id)
    {
        var machine = await dbContext.Machines.Include(m => m.Sector).FirstOrDefaultAsync(m => m.Id == id);
        if (machine == null) return NotFound();
        return Ok(machine);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMachine(Guid id)
    {
        var machine = await dbContext.Machines.FindAsync(id);
        if (machine == null) return NotFound();
        dbContext.Machines.Remove(machine);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var total = await dbContext.Machines.CountAsync();
        var online = await dbContext.Machines.CountAsync(m => m.ConnectionStatus == Crive.Shared.Enums.ConnectionStatus.Online);
        var offline = total - online;

        return Ok(new { Total = total, Online = online, Offline = offline });
    }
}

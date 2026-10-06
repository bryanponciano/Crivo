using Crive.Api.Infrastructure;
using Crive.Api.Models;
using Crive.Api.Services;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetupController(AppDbContext dbContext, AuthService authService) : ControllerBase
{
    [HttpGet("sectors")]
    public async Task<IActionResult> GetSectors([FromQuery] string tenantCode)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == tenantCode && t.IsActive);
            
        if (tenant == null) return NotFound("Tenant not found");

        var sectors = await dbContext.Sectors
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenant.Id && s.IsActive)
            .Select(s => new SectorListItemDto { Id = s.Id, Name = s.Name })
            .ToListAsync();

        return Ok(sectors);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterMachine([FromBody] MachineRegistrationDto request)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == request.TenantCode && t.IsActive);

        if (tenant == null) return NotFound("Tenant not found");

        var machine = await dbContext.Machines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.HardwareFingerprint == request.HardwareFingerprint && m.TenantId == tenant.Id);

        if (machine == null)
        {
            machine = new Machine
            {
                TenantId = tenant.Id,
                SectorId = request.SectorId,
                HardwareFingerprint = request.HardwareFingerprint,
                Hostname = request.Hostname,
                AssetNumber = request.AssetNumber,
                EmployeeName = request.EmployeeName,
                AgentVersion = request.AgentVersion,
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                ConnectionStatus = Crive.Shared.Enums.ConnectionStatus.Online
            };
            dbContext.Machines.Add(machine);
        }
        else
        {
            machine.Hostname = request.Hostname;
            machine.AssetNumber = request.AssetNumber;
            machine.EmployeeName = request.EmployeeName;
            machine.AgentVersion = request.AgentVersion;
            machine.SectorId = request.SectorId;
            machine.LastHeartbeat = DateTime.UtcNow;
            machine.ConnectionStatus = Crive.Shared.Enums.ConnectionStatus.Online;
        }

        await dbContext.SaveChangesAsync();
        var token = authService.GenerateDeviceToken(machine);

        return Ok(new MachineRegistrationResponseDto 
        { 
            DeviceToken = token, 
            MachineId = machine.Id,
            InitialRules = new RulePackageDto { RuleHash = string.Empty, Rules = [], CompiledAt = DateTime.UtcNow }
        });
    }
}

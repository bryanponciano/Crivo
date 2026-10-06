using Crive.Api.Infrastructure;
using Crive.Api.Models;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SectorsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSectors()
    {
        var sectors = await dbContext.Sectors.Where(s => s.IsActive).ToListAsync();
        var dtos = sectors.Select(s => new SectorDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSector([FromBody] SectorDto dto)
    {
        var sector = new Sector
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Sectors.Add(sector);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSectors), new { id = sector.Id }, sector);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSector(Guid id, [FromBody] SectorDto dto)
    {
        var sector = await dbContext.Sectors.FindAsync(id);
        if (sector == null) return NotFound();

        sector.Name = dto.Name;
        sector.Description = dto.Description;
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSector(Guid id)
    {
        var sector = await dbContext.Sectors.FindAsync(id);
        if (sector == null) return NotFound();

        sector.IsActive = false;
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}

using Crive.Api.Infrastructure;
using Crive.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TenantsController(AppDbContext dbContext, AuthService authService, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var tenant = await dbContext.Tenants.FindAsync(tenantContext.CurrentTenantId);
        if (tenant == null) return NotFound();

        return Ok(new 
        { 
            tenant.Name,
            tenant.Slug,
            HasUninstallPassword = !string.IsNullOrEmpty(tenant.UninstallPasswordHash)
        });
    }

    [HttpPut("uninstall-password")]
    public async Task<IActionResult> UpdateUninstallPassword([FromBody] string password)
    {
        var tenant = await dbContext.Tenants.FindAsync(tenantContext.CurrentTenantId);
        if (tenant == null) return NotFound();

        tenant.UninstallPasswordHash = authService.HashPassword(password);
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}

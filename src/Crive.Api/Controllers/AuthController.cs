using Crive.Api.Services;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Crive.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService, AppDbContext dbContext) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await dbContext.Users
            .Include(u => u.Tenant)
            .IgnoreQueryFilters() // Allow finding user before tenant context is set
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !authService.ValidatePassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = authService.GenerateJwtToken(user);
        return Ok(new LoginResponseDto 
        { 
            Token = token,
            UserName = user.Name,
            Role = user.Role.ToString(),
            TenantName = user.Tenant?.Name ?? "Unknown",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
    }
}

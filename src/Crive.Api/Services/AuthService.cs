using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Crive.Api.Infrastructure;
using Crive.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Crive.Api.Services;

public class AuthService(IConfiguration configuration, AppDbContext dbContext)
{
    public string GenerateJwtToken(User user)
    {
        var secret = configuration["Jwt:Secret"] ?? "super_secret_key_that_should_be_long_enough_for_hmacsha256";
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "Crive",
            audience: configuration["Jwt:Audience"] ?? "Crive",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateDeviceToken(Machine machine)
    {
        var secret = configuration["Jwt:Secret"] ?? "super_secret_key_that_should_be_long_enough_for_hmacsha256";
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        
        var claims = new[]
        {
            new Claim("machine_id", machine.Id.ToString()),
            new Claim("tenant_id", machine.TenantId.ToString()),
            new Claim(ClaimTypes.Role, "Agent")
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "Crive",
            audience: configuration["Jwt:Audience"] ?? "Crive",
            claims: claims,
            expires: DateTime.UtcNow.AddYears(10), // Long-lived for device
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool ValidatePassword(string password, string hash)
    {
        try 
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch 
        {
            return false;
        }
    }

    public async Task<bool> ValidateUninstallPasswordAsync(Guid tenantId, string password)
    {
        var tenant = await dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.UninstallPasswordHash)) return false;

        return ValidatePassword(password, tenant.UninstallPasswordHash);
    }
}

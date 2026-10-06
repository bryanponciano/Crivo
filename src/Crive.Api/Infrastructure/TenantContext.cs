using System.Security.Claims;

namespace Crive.Api.Infrastructure;

public interface ITenantContext
{
    Guid CurrentTenantId { get; set; }
}

public class TenantContext : ITenantContext
{
    public Guid CurrentTenantId { get; set; }
}

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            tenantContext.CurrentTenantId = tenantId;
        }

        await next(context);
    }
}

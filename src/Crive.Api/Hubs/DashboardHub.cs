using Crive.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Crive.Api.Hubs;

[Authorize]
public class DashboardHub : Hub<IDashboardClient>
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
        }

        await base.OnConnectedAsync();
    }
}

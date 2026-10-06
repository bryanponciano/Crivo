using Crive.Api.Services;
using Crive.Shared.Contracts;
using Crive.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Crive.Api.Hubs;

[Authorize]
public class AgentHub(MachineService machineService, RuleCompilationService ruleCompilationService, AuthService authService, ILogger<AgentHub> logger) 
    : Hub<ICriveAgentClient>, ICriveHub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
        }

        var machineId = Context.User?.FindFirst("machine_id")?.Value;
        if (Guid.TryParse(machineId, out var id))
        {
            await machineService.UpdateHeartbeatAsync(id, Crive.Shared.Enums.ConnectionStatus.Online);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var machineId = Context.User?.FindFirst("machine_id")?.Value;
        if (Guid.TryParse(machineId, out var id))
        {
            await machineService.UpdateHeartbeatAsync(id, Crive.Shared.Enums.ConnectionStatus.Offline);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<MachineRegistrationResponseDto> RegisterMachine(MachineRegistrationDto registration)
    {
        // Se a lógica de registro precisar ser suportada via SignalR no futuro,
        // deverá ser implementada aqui. Normalmente é feito via SetupController (HTTP).
        return await Task.FromResult(new MachineRegistrationResponseDto 
        { 
            MachineId = Guid.Empty, 
            DeviceToken = string.Empty, 
            InitialRules = new RulePackageDto { RuleHash = string.Empty, Rules = [], CompiledAt = DateTime.UtcNow } 
        });
    }

    public async Task<HeartbeatResponseDto> SendHeartbeat(HeartbeatDto heartbeat)
    {
        var machineIdStr = Context.User?.FindFirst("machine_id")?.Value;
        if (!Guid.TryParse(machineIdStr, out var machineId)) 
            return new HeartbeatResponseDto { RulesOutOfSync = false };

        await machineService.UpdateHeartbeatAsync(machineId, Crive.Shared.Enums.ConnectionStatus.Online, heartbeat.ActiveRuleHash);

        var currentPackage = await ruleCompilationService.CompileRulesForMachineAsync(machineId);
        var expectedHash = ruleCompilationService.ComputeRuleHash(currentPackage);

        if (heartbeat.ActiveRuleHash != expectedHash)
        {
            await Clients.Caller.ReceiveRuleUpdate(currentPackage);
            return new HeartbeatResponseDto { RulesOutOfSync = true, UpdatedRules = currentPackage };
        }

        return new HeartbeatResponseDto { RulesOutOfSync = false };
    }

    public async Task ReportBlockEvent(BlockEventDto blockEvent)
    {
        var machineIdStr = Context.User?.FindFirst("machine_id")?.Value;
        if (!Guid.TryParse(machineIdStr, out var machineId)) return;

        await machineService.RecordBlockEventAsync(machineId, blockEvent);
    }

    public async Task<bool> ValidateUninstallPassword(Guid machineId, string password)
    {
        var tenantIdStr = Context.User?.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId)) return false;

        return await authService.ValidateUninstallPasswordAsync(tenantId, password);
    }
}

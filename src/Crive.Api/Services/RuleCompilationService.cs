using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Crive.Api.Infrastructure;
using Crive.Api.Hubs;
using Crive.Shared.Contracts;
using Crive.Shared.DTOs;
using Crive.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Services;

public class RuleCompilationService(AppDbContext dbContext, IHubContext<AgentHub, ICriveAgentClient> agentHub)
{
    public async Task<RulePackageDto> CompileRulesForMachineAsync(Guid machineId)
    {
        var machine = await dbContext.Machines
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == machineId);

        if (machine == null) return new RulePackageDto { RuleHash = string.Empty, Rules = [], CompiledAt = DateTime.UtcNow };

        var rules = await dbContext.Rules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == machine.TenantId && r.IsActive)
            .Where(r => r.ScopeType == ScopeType.Global ||
                        (r.ScopeType == ScopeType.Sector && r.ScopeId == machine.SectorId) ||
                        (r.ScopeType == ScopeType.Machine && r.ScopeId == machine.Id))
            .ToListAsync();

        var compiledRules = rules.Select(r => new CompiledRuleDto
        {
            RuleId = r.Id,
            Name = r.Name,
            ScopeType = r.ScopeType,
            Action = r.Action,
            Priority = r.Priority,
            Domains = r.Domains ?? [],
            IsTemporary = r.IsTemporary,
            ExpiresAt = r.ExpiresAt
        }).OrderByDescending(r => r.Priority).ToList();

        var hash = ComputeRulesHash(compiledRules);
        
        return new RulePackageDto 
        { 
            RuleHash = hash,
            Rules = compiledRules,
            CompiledAt = DateTime.UtcNow
        };
    }

    public string ComputeRuleHash(RulePackageDto package)
    {
        return ComputeRulesHash(package.Rules);
    }

    private string ComputeRulesHash(List<CompiledRuleDto> rules)
    {
        var json = JsonSerializer.Serialize(rules);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    public Task PushRulesToAffectedMachines(Guid ruleId)
    {
        // Simple approach: trigger update for all machines in tenant for simplicity,
        // or filter by rule scope. Here we assume we resolve machines and push.
        // For demonstration, we could just send a signal to clients in the tenant group.
        // More specific logic can be added.
        return Task.CompletedTask;
    }
}

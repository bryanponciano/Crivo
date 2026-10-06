using Crive.Api.Infrastructure;
using Crive.Api.Models;
using Crive.Api.Services;
using Crive.Shared.DTOs;
using Crive.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RulesController(AppDbContext dbContext, RuleCompilationService ruleCompilationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRules([FromQuery] ScopeType? scopeType)
    {
        var query = dbContext.Rules.AsQueryable();
        if (scopeType.HasValue)
            query = query.Where(r => r.ScopeType == scopeType.Value);

        var rules = await query.ToListAsync();
        var dtos = rules.Select(r => new RuleDto
        {
            Id = r.Id,
            Name = r.Name,
            ScopeType = r.ScopeType,
            ScopeId = r.ScopeId,
            Action = r.Action,
            Priority = r.Priority,
            IsActive = r.IsActive,
            IsTemporary = r.IsTemporary,
            ExpiresAt = r.ExpiresAt,
            Domains = r.Domains
        });
        
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleDto dto)
    {
        var rule = new Rule
        {
            Name = dto.Name,
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Action = dto.Action,
            Priority = dto.Priority,
            Domains = dto.Domains,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.Rules.Add(rule);
        await dbContext.SaveChangesAsync();

        await ruleCompilationService.PushRulesToAffectedMachines(rule.Id);

        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CreateRuleDto dto)
    {
        var rule = await dbContext.Rules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.Name = dto.Name;
        rule.ScopeType = dto.ScopeType;
        rule.ScopeId = dto.ScopeId;
        rule.Action = dto.Action;
        rule.Priority = dto.Priority;
        rule.Domains = dto.Domains;
        rule.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await ruleCompilationService.PushRulesToAffectedMachines(rule.Id);
        
        return Ok();
    }

    [HttpPut("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleRule(Guid id)
    {
        var rule = await dbContext.Rules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await ruleCompilationService.PushRulesToAffectedMachines(rule.Id);

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await dbContext.Rules.FindAsync(id);
        if (rule == null) return NotFound();

        dbContext.Rules.Remove(rule);
        await dbContext.SaveChangesAsync();
        
        await ruleCompilationService.PushRulesToAffectedMachines(id);

        return Ok();
    }
}

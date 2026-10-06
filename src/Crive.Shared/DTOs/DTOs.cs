using Crive.Shared.Enums;
using MessagePack;

namespace Crive.Shared.DTOs;

// ============================================================
// DTOs de Registro e Comunicação Agente <-> Servidor
// ============================================================

/// <summary>
/// Dados enviados pelo agente ao se registrar pela primeira vez.
/// </summary>
[MessagePackObject]
public record MachineRegistrationDto
{
    [Key(0)] public required string AssetNumber { get; init; }
    [Key(1)] public required string EmployeeName { get; init; }
    [Key(2)] public required Guid SectorId { get; init; }
    [Key(3)] public required string Hostname { get; init; }
    [Key(4)] public required string HardwareFingerprint { get; init; }
    [Key(5)] public required string AgentVersion { get; init; }
    [Key(6)] public required string TenantCode { get; init; }
}

/// <summary>
/// Resposta do servidor após registro bem-sucedido da máquina.
/// </summary>
[MessagePackObject]
public record MachineRegistrationResponseDto
{
    [Key(0)] public required Guid MachineId { get; init; }
    [Key(1)] public required string DeviceToken { get; init; }
    [Key(2)] public required RulePackageDto InitialRules { get; init; }
}

/// <summary>
/// Heartbeat periódico enviado pelo agente ao servidor.
/// </summary>
[MessagePackObject]
public record HeartbeatDto
{
    [Key(0)] public required Guid MachineId { get; init; }
    [Key(1)] public required string ActiveRuleHash { get; init; }
    [Key(2)] public required string AgentVersion { get; init; }
    [Key(3)] public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Resposta do servidor ao heartbeat — indica se as regras precisam ser atualizadas.
/// </summary>
[MessagePackObject]
public record HeartbeatResponseDto
{
    [Key(0)] public required bool RulesOutOfSync { get; init; }
    [Key(1)] public RulePackageDto? UpdatedRules { get; init; }
}

// ============================================================
// DTOs de Regras de Bloqueio
// ============================================================

/// <summary>
/// Pacote completo de regras compiladas para uma máquina específica.
/// Enviado pelo servidor ao agente via SignalR.
/// </summary>
[MessagePackObject]
public record RulePackageDto
{
    [Key(0)] public required string RuleHash { get; init; }
    [Key(1)] public required List<CompiledRuleDto> Rules { get; init; }
    [Key(2)] public required DateTime CompiledAt { get; init; }
}

/// <summary>
/// Uma regra individual compilada e pronta para avaliação no agente.
/// </summary>
[MessagePackObject]
public record CompiledRuleDto
{
    [Key(0)] public required Guid RuleId { get; init; }
    [Key(1)] public required string Name { get; init; }
    [Key(2)] public required RuleAction Action { get; init; }
    [Key(3)] public required ScopeType ScopeType { get; init; }
    [Key(4)] public required int Priority { get; init; }
    [Key(5)] public required List<string> Domains { get; init; }
    [Key(6)] public bool IsTemporary { get; init; }
    [Key(7)] public DateTime? ExpiresAt { get; init; }
}

// ============================================================
// DTOs de Eventos de Bloqueio
// ============================================================

/// <summary>
/// Evento reportado pelo agente quando um domínio é bloqueado.
/// </summary>
[MessagePackObject]
public record BlockEventDto
{
    [Key(0)] public required Guid MachineId { get; init; }
    [Key(1)] public required Guid RuleId { get; init; }
    [Key(2)] public required string BlockedDomain { get; init; }
    [Key(3)] public required DateTime BlockedAt { get; init; }
    [Key(4)] public string? ProcessName { get; init; }
}

// ============================================================
// DTOs do Painel Web (API REST)
// ============================================================

/// <summary>
/// Dados para login no painel web.
/// </summary>
public record LoginRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Resposta de login com token JWT.
/// </summary>
public record LoginResponseDto
{
    public required string Token { get; init; }
    public required string UserName { get; init; }
    public required string Role { get; init; }
    public required string TenantName { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// DTO para criar/atualizar setor.
/// </summary>
public record SectorDto
{
    public Guid? Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public int MachineCount { get; init; }
}

/// <summary>
/// DTO de máquina para exibição no painel.
/// </summary>
public record MachineDto
{
    public required Guid Id { get; init; }
    public required string AssetNumber { get; init; }
    public required string EmployeeName { get; init; }
    public required string SectorName { get; init; }
    public required Guid SectorId { get; init; }
    public required string Hostname { get; init; }
    public required ConnectionStatus Status { get; init; }
    public required string AgentVersion { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public DateTime RegisteredAt { get; init; }
}

/// <summary>
/// DTO para criar/atualizar regra de bloqueio via painel.
/// </summary>
public record CreateRuleDto
{
    public required string Name { get; init; }
    public required ScopeType ScopeType { get; init; }
    public Guid? ScopeId { get; init; }
    public required RuleAction Action { get; init; }
    public required List<string> Domains { get; init; }
    public int Priority { get; init; } = 100;
    public bool IsTemporary { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// DTO de regra para exibição no painel.
/// </summary>
public record RuleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required ScopeType ScopeType { get; init; }
    public Guid? ScopeId { get; init; }
    public string? ScopeName { get; init; }
    public required RuleAction Action { get; init; }
    public required List<string> Domains { get; init; }
    public required int Priority { get; init; }
    public required bool IsActive { get; init; }
    public bool IsTemporary { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para exibição de evento de bloqueio no painel.
/// </summary>
public record BlockEventViewDto
{
    public required string MachineName { get; init; }
    public required string EmployeeName { get; init; }
    public required string BlockedDomain { get; init; }
    public required string RuleName { get; init; }
    public required DateTime BlockedAt { get; init; }
    public string? ProcessName { get; init; }
}

/// <summary>
/// Dados resumidos para o dashboard principal.
/// </summary>
public record DashboardSummaryDto
{
    public required int TotalMachines { get; init; }
    public required int OnlineMachines { get; init; }
    public required int OfflineMachines { get; init; }
    public required int TotalBlocksToday { get; init; }
    public required int ActiveRules { get; init; }
    public required int TotalSectors { get; init; }
    public required List<BlockEventViewDto> RecentBlocks { get; init; }
}

/// <summary>
/// Dados para listagem de setores no dropdown do setup do agente.
/// </summary>
public record SectorListItemDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

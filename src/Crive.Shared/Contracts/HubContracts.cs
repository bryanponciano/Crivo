using Crive.Shared.DTOs;

namespace Crive.Shared.Contracts;

/// <summary>
/// Interface do Hub SignalR — métodos que o SERVIDOR expõe para o AGENTE chamar.
/// </summary>
public interface ICriveHub
{
    /// <summary>Registra uma nova máquina no sistema.</summary>
    Task<MachineRegistrationResponseDto> RegisterMachine(MachineRegistrationDto registration);

    /// <summary>Envia heartbeat periódico com o hash das regras ativas.</summary>
    Task<HeartbeatResponseDto> SendHeartbeat(HeartbeatDto heartbeat);

    /// <summary>Reporta um evento de bloqueio de domínio.</summary>
    Task ReportBlockEvent(BlockEventDto blockEvent);

    /// <summary>Valida a senha de desinstalação contra o servidor.</summary>
    Task<bool> ValidateUninstallPassword(Guid machineId, string password);
}

/// <summary>
/// Interface do cliente SignalR — métodos que o AGENTE expõe para o SERVIDOR chamar.
/// O servidor invoca esses métodos remotamente no agente via SignalR.
/// </summary>
public interface ICriveAgentClient
{
    /// <summary>Recebe pacote atualizado de regras de bloqueio.</summary>
    Task ReceiveRuleUpdate(RulePackageDto rules);

    /// <summary>Força o agente a solicitar uma sincronização completa de regras.</summary>
    Task ForceRuleSync();

    /// <summary>Solicita informações de diagnóstico do agente.</summary>
    Task<string> GetDiagnostics();
}

/// <summary>
/// Interface do Hub do Dashboard — métodos para atualização em tempo real do painel web.
/// </summary>
public interface IDashboardClient
{
    /// <summary>Notifica o painel que uma máquina mudou de status.</summary>
    Task MachineStatusChanged(Guid machineId, string status);

    /// <summary>Notifica o painel sobre um novo evento de bloqueio.</summary>
    Task NewBlockEvent(BlockEventViewDto blockEvent);

    /// <summary>Notifica o painel que uma regra foi sincronizada com sucesso em uma máquina.</summary>
    Task RuleSyncConfirmed(Guid machineId, string ruleHash);
}

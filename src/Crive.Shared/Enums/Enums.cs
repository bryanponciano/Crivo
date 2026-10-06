using MessagePack;

namespace Crive.Shared.Enums;

/// <summary>
/// Tipo de escopo de aplicação de uma regra de bloqueio.
/// </summary>
public enum ScopeType
{
    /// <summary>Regra aplicada a toda a empresa.</summary>
    Global = 0,

    /// <summary>Regra aplicada a um setor/departamento específico.</summary>
    Sector = 1,

    /// <summary>Exceção individual para uma máquina específica.</summary>
    Machine = 2
}

/// <summary>
/// Ação a ser tomada quando uma regra é correspondida.
/// </summary>
public enum RuleAction
{
    /// <summary>Bloqueia o acesso ao domínio.</summary>
    Block = 0,

    /// <summary>Libera o acesso ao domínio (usado para exceções).</summary>
    Allow = 1
}

/// <summary>
/// Status de conexão de uma máquina com o servidor.
/// </summary>
public enum ConnectionStatus
{
    Online = 0,
    Offline = 1,
    Stale = 2
}

/// <summary>
/// Papel do usuário no painel web.
/// </summary>
public enum UserRole
{
    Admin = 0,
    Manager = 1,
    Viewer = 2
}

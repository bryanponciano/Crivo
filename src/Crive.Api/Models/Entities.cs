using Crive.Shared.Enums;

namespace Crive.Api.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string UninstallPasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Sector> Sectors { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Machine> Machines { get; set; } = [];
    public ICollection<Rule> Rules { get; set; } = [];
}

public class Sector
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Machine> Machines { get; set; } = [];
}

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public class Machine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SectorId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string HardwareFingerprint { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public ConnectionStatus ConnectionStatus { get; set; }
    public string ActiveRuleHash { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Sector? Sector { get; set; }
}

public class Rule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ScopeType ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public RuleAction Action { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool IsTemporary { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Domains { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public class BlockEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MachineId { get; set; }
    public Guid? RuleId { get; set; }
    public string BlockedDomain { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public string ProcessName { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;
    public Machine Machine { get; set; } = null!;
    public Rule? Rule { get; set; }
}

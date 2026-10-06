using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Crive.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Communication;

public class OfflineCache
{
    private static readonly string CacheFile;
    private static readonly string ConfigFile;

    static OfflineCache()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(appData, "Crive");
        Directory.CreateDirectory(dir);
        
        CacheFile = Path.Combine(dir, "rules.cache");
        ConfigFile = Path.Combine(dir, "config.dat");
    }

    public static bool ConfigExists() => File.Exists(ConfigFile);

    public bool IsConfigured() => File.Exists(ConfigFile);

    public string? GetDeviceToken()
    {
        return LoadConfig()?.DeviceToken;
    }

    public Guid GetMachineId()
    {
        return LoadConfig()?.MachineId ?? Guid.Empty;
    }

    public void SaveConfig(AgentConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(ConfigFile, encrypted);
    }

    private AgentConfig? LoadConfig()
    {
        if (!File.Exists(ConfigFile)) return null;
        try
        {
            var encrypted = File.ReadAllBytes(ConfigFile);
            var jsonBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
            return JsonSerializer.Deserialize<AgentConfig>(Encoding.UTF8.GetString(jsonBytes));
        }
        catch
        {
            return null;
        }
    }

    public Task<AgentConfig?> LoadConfigAsync()
    {
        return Task.FromResult(LoadConfig());
    }

    public void SaveRules(RulePackageDto package)
    {
        var json = JsonSerializer.Serialize(package);
        File.WriteAllText(CacheFile, json);
    }

    public Task SaveRulesAsync(RulePackageDto package)
    {
        SaveRules(package);
        return Task.CompletedTask;
    }

    public RulePackageDto? LoadRules()
    {
        if (!File.Exists(CacheFile)) return null;
        try
        {
            var json = File.ReadAllText(CacheFile);
            return JsonSerializer.Deserialize<RulePackageDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public Task<RulePackageDto?> LoadRulesAsync()
    {
        return Task.FromResult(LoadRules());
    }

    public string GetRuleHash()
    {
        var package = LoadRules();
        return package?.RuleHash ?? string.Empty;
    }
}

public class AgentConfig
{
    public string ServerUrl { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public Guid MachineId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}

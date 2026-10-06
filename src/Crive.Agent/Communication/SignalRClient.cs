using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Crive.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Crive.Shared.DTOs;
using Crive.Agent.Engine;

namespace Crive.Agent.Communication;

public class CriveSignalRClient : ICriveAgentClient
{
    private readonly ILogger<CriveSignalRClient> _logger;
    private readonly RuleEvaluator _ruleEvaluator;
    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public CriveSignalRClient(ILogger<CriveSignalRClient> logger, RuleEvaluator ruleEvaluator)
    {
        _logger = logger;
        _ruleEvaluator = ruleEvaluator;
    }

    public async Task ConnectAsync(string serverUrl, string deviceToken, CancellationToken ct)
    {
        var url = $"{serverUrl.TrimEnd('/')}/hubs/agent";

        _connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(deviceToken);
            })
            .AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        _connection.On<RulePackageDto>("ReceiveRuleUpdate", ReceiveRuleUpdate);
        _connection.On("ForceRuleSync", ForceRuleSync);
        _connection.On("GetDiagnostics", GetDiagnostics);

        await _connection.StartAsync(ct);
        _logger.LogInformation("SignalR connected to {Url}", url);
    }

    public Task ReceiveRuleUpdate(RulePackageDto package)
    {
        _logger.LogInformation("Received rule update. Hash: {Hash}", package.RuleHash);
        // The worker is supposed to save to cache, but if pushed here, we only evaluate.
        // Usually, the server sends this and the client applies it immediately.
        _ruleEvaluator.LoadRules(package);
        return Task.CompletedTask;
    }

    public Task ForceRuleSync()
    {
        _logger.LogInformation("Force rule sync requested by server.");
        // We could trigger a heartbeat to force sync
        return Task.CompletedTask;
    }

    public Task<string> GetDiagnostics()
    {
        return Task.FromResult($"Agent online. Current rules hash: {_ruleEvaluator.CurrentRuleHash}");
    }

    public async Task<HeartbeatResponseDto?> SendHeartbeatAsync(string activeRuleHash)
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
            return null;

        var heartbeat = new HeartbeatDto
        {
            MachineId = Guid.Empty, // The server gets it from the JWT Claims
            ActiveRuleHash = activeRuleHash,
            AgentVersion = "1.0.0",
            Timestamp = DateTime.UtcNow
        };

        return await _connection.InvokeAsync<HeartbeatResponseDto>("SendHeartbeat", heartbeat);
    }

    public async Task RequestRuleSyncAsync()
    {
        await SendHeartbeatAsync(_ruleEvaluator.CurrentRuleHash);
    }

    public async Task ReportBlockEventAsync(string domain, CompiledRuleDto? rule)
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
            return;

        var blockEvent = new BlockEventDto
        {
            MachineId = Guid.Empty, // from JWT
            RuleId = rule?.RuleId ?? Guid.Empty,
            BlockedDomain = domain,
            BlockedAt = DateTime.UtcNow,
            ProcessName = null
        };

        await _connection.SendAsync("ReportBlockEvent", blockEvent);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}

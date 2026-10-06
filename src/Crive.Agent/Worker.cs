using Crive.Agent.Communication;
using Crive.Agent.Engine;
using Crive.Agent.Protection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crive.Agent;

/// <summary>
/// Worker principal do agente Crivo. Orquestra todas as camadas:
/// 1. Carrega regras do cache local (funciona offline)
/// 2. Configura DNS do sistema para usar nosso proxy local
/// 3. Instala filtros WFP (anti-bypass: DoT, DoH, VPN)
/// 4. Inicia interceptor DNS (proxy na porta 5353)
/// 5. Inicia servidor de block page (HTTP na porta 8080)
/// 6. Conecta ao servidor via SignalR (recebe atualizações de regras)
/// 7. Loop de heartbeat a cada 30 segundos
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly WfpFilterEngine _wfpEngine;
    private readonly DnsInterceptor _dnsInterceptor;
    private readonly BlockPageServer _blockPageServer;
    private readonly CriveSignalRClient _signalRClient;
    private readonly OfflineCache _offlineCache;
    private readonly BlockEventReporter _blockReporter;
    private readonly SelfProtection _selfProtection;

    private const int HeartbeatIntervalSeconds = 30;

    public Worker(
        ILogger<Worker> logger,
        RuleEvaluator ruleEvaluator,
        WfpFilterEngine wfpEngine,
        DnsInterceptor dnsInterceptor,
        BlockPageServer blockPageServer,
        CriveSignalRClient signalRClient,
        OfflineCache offlineCache,
        BlockEventReporter blockReporter,
        SelfProtection selfProtection)
    {
        _logger = logger;
        _ruleEvaluator = ruleEvaluator;
        _wfpEngine = wfpEngine;
        _dnsInterceptor = dnsInterceptor;
        _blockPageServer = blockPageServer;
        _signalRClient = signalRClient;
        _offlineCache = offlineCache;
        _blockReporter = blockReporter;
        _selfProtection = selfProtection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Crivo Agent iniciando... ===");

        try
        {
            // 1. Aplicar proteções do serviço
            _selfProtection.Apply();

            // 2. Carregar regras do cache local (permite funcionar offline)
            var cachedRules = await _offlineCache.LoadRulesAsync();
            if (cachedRules != null)
            {
                _ruleEvaluator.LoadRules(cachedRules);
                _logger.LogInformation("Regras carregadas do cache local (hash: {Hash})", cachedRules.RuleHash);
            }
            else
            {
                _logger.LogWarning("Sem regras em cache — aguardando sincronização com servidor");
            }

            // 3. Inicializar e instalar filtros WFP
            _wfpEngine.Initialize();
            _wfpEngine.InstallDnsRedirect();
            _wfpEngine.InstallAntiBypass();

            // 4. Configurar DNS do sistema para apontar para nosso proxy
            _wfpEngine.SetLocalDns();

            // 5. Iniciar DNS Interceptor (porta 5353)
            _ = _dnsInterceptor.StartAsync(stoppingToken);

            // 6. Iniciar Block Page Server (porta 8080)
            _ = _blockPageServer.StartAsync(stoppingToken);

            // 7. Conectar ao servidor via SignalR
            await ConnectToServerAsync(stoppingToken);

            // 8. Loop de heartbeat
            _logger.LogInformation("=== Crivo Agent operacional — iniciando loop de heartbeat ===");
            await HeartbeatLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Crivo Agent parando (cancelado)...");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Erro fatal no Crivo Agent");
            throw;
        }
        finally
        {
            await ShutdownAsync();
        }
    }

    private async Task ConnectToServerAsync(CancellationToken ct)
    {
        var config = await _offlineCache.LoadConfigAsync();
        if (config == null)
        {
            _logger.LogWarning("Configuração não encontrada — agente não registrado. Execute o setup primeiro.");
            return;
        }

        try
        {
            await _signalRClient.ConnectAsync(config.ServerUrl, config.DeviceToken, ct);
            _logger.LogInformation("Conectado ao servidor Crivo");

            // Solicitar sincronização de regras ao conectar
            await _signalRClient.RequestRuleSyncAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível conectar ao servidor — operando com regras em cache");
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(HeartbeatIntervalSeconds), ct);

                // Enviar heartbeat ao servidor
                if (_signalRClient.IsConnected)
                {
                    var response = await _signalRClient.SendHeartbeatAsync(
                        _ruleEvaluator.CurrentRuleHash);

                    // Se o servidor indicar que as regras estão desatualizadas
                    if (response?.RulesOutOfSync == true && response.UpdatedRules != null)
                    {
                        _ruleEvaluator.LoadRules(response.UpdatedRules);
                        await _offlineCache.SaveRulesAsync(response.UpdatedRules);
                        _logger.LogInformation("Regras atualizadas pelo servidor (hash: {Hash})",
                            response.UpdatedRules.RuleHash);
                    }

                    // Enviar eventos de bloqueio pendentes
                    var pendingEvents = _blockReporter.DrainEvents();
                    foreach (var evt in pendingEvents)
                    {
                        await _signalRClient.ReportBlockEventAsync(evt.Domain, evt.Rule);
                    }
                }
                else
                {
                    // Tentar reconectar
                    _logger.LogDebug("SignalR desconectado — tentando reconexão...");
                    await ConnectToServerAsync(ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no heartbeat loop");
            }
        }
    }

    private async Task ShutdownAsync()
    {
        _logger.LogInformation("=== Crivo Agent desligando... ===");

        // Restaurar DNS do sistema
        _wfpEngine.RestoreDns();

        // Remover filtros WFP
        _wfpEngine.RemoveAllFilters();

        // Parar interceptores
        _dnsInterceptor.Stop();
        _blockPageServer.Stop();

        // Desconectar SignalR
        await _signalRClient.DisconnectAsync();

        // Limpar recursos
        _wfpEngine.Dispose();
        _dnsInterceptor.Dispose();
        _blockPageServer.Dispose();

        _logger.LogInformation("=== Crivo Agent desligado ===");
    }
}

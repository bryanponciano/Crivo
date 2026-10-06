using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Crive.Shared.DTOs;

namespace Crive.Agent.Engine;

/// <summary>
/// Proxy DNS local que intercepta todas as queries DNS redirecionadas pela WFP.
/// Opera na porta 5353 (UDP). O WFP redireciona todo tráfego UDP porta 53 para cá.
/// 
/// Fluxo:
/// 1. Recebe query DNS do sistema operacional
/// 2. Parseia o QNAME (domínio consultado)
/// 3. Consulta o RuleEvaluator
/// 4. Se BLOQUEADO: responde com A record apontando para 127.0.0.1 (sinkhole → block page)
/// 5. Se PERMITIDO: encaminha para o DNS upstream real e retorna a resposta
/// </summary>
public sealed class DnsInterceptor : IDisposable
{
    private readonly ILogger<DnsInterceptor> _logger;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly BlockEventReporter _blockReporter;
    private readonly Crive.Agent.Communication.NotificationService _notificationService;
    private UdpClient? _listener;
    private bool _running;
    private readonly IPEndPoint _upstreamDns = new(IPAddress.Parse("8.8.8.8"), 53);
    private readonly int _listenPort;

    // IP para sinkhole — aponta para nosso BlockPageServer (127.0.0.1)
    private static readonly byte[] SinkholeIpBytes = [127, 0, 0, 1];

    public DnsInterceptor(
        ILogger<DnsInterceptor> logger,
        RuleEvaluator ruleEvaluator,
        BlockEventReporter blockReporter,
        Crive.Agent.Communication.NotificationService notificationService,
        int listenPort = 53)
    {
        _logger = logger;
        _ruleEvaluator = ruleEvaluator;
        _blockReporter = blockReporter;
        _notificationService = notificationService;
        _listenPort = listenPort;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _running = true;
        _listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, _listenPort));
        _logger.LogInformation("DNS Interceptor iniciado em 127.0.0.1:{Port}", _listenPort);
        return Task.Run(() => ListenLoopAsync(ct), ct);
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (_running && !ct.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _listener!.ReceiveAsync(ct);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                // Ignorar erro 10054 no Windows causado por ICMP Port Unreachable de respostas assíncronas
                continue;
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop do DNS Interceptor");
                await Task.Delay(1000, ct);
                continue;
            }

            // Processar cada query em background para não bloquear o listener
            _ = Task.Run(() => ProcessQueryAsync(result.Buffer, result.RemoteEndPoint, ct), ct);
        }
    }

    private async Task ProcessQueryAsync(byte[] queryData, IPEndPoint clientEndpoint, CancellationToken ct)
    {
        try
        {
            // Parsear o QNAME da query DNS
            var domain = ParseDnsQName(queryData);
            if (string.IsNullOrEmpty(domain))
            {
                // Não conseguiu parsear — encaminha para upstream
                await ForwardToUpstream(queryData, clientEndpoint, ct);
                return;
            }

            // Avaliar contra as regras de bloqueio
            var result = _ruleEvaluator.Evaluate(domain);

            if (result.IsBlocked)
            {
                _logger.LogInformation("🚫 DNS bloqueado: {Domain} (regra: {Rule})", domain, result.MatchedRule?.Name);

                // Construir resposta DNS com sinkhole (127.0.0.1)
                var response = BuildSinkholeResponse(queryData, domain);
                await _listener!.SendAsync(response, response.Length, clientEndpoint);

                // Reportar evento de bloqueio e exibir notificação
                _blockReporter.Report(domain, result.MatchedRule);
                _ = System.Threading.Tasks.Task.Run(() => _notificationService.ShowBlockNotification(domain));
            }
            else
            {
                // Encaminhar para DNS upstream real
                await ForwardToUpstream(queryData, clientEndpoint, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar query DNS.");
        }
    }

    /// <summary>
    /// Parseia o QNAME (domínio consultado) de um pacote DNS.
    /// Formato DNS: [len][label][len][label]...[0]
    /// Ex: [3]www[6]google[3]com[0] → "www.google.com"
    /// </summary>
    private static string? ParseDnsQName(byte[] data)
    {
        if (data.Length < 12) return null; // Header mínimo DNS = 12 bytes

        // QDCOUNT deve ser >= 1
        var qdCount = (data[4] << 8) | data[5];
        if (qdCount < 1) return null;

        int offset = 12; // Pula o header DNS (12 bytes)
        var labels = new List<string>();

        while (offset < data.Length)
        {
            int labelLen = data[offset++];

            if (labelLen == 0) break; // Fim do QNAME

            // Proteção contra compression pointers (0xC0) — não esperado em queries originais
            if ((labelLen & 0xC0) == 0xC0) break;

            if (offset + labelLen > data.Length) return null;

            var label = System.Text.Encoding.ASCII.GetString(data, offset, labelLen);
            labels.Add(label);
            offset += labelLen;
        }

        return labels.Count > 0 ? string.Join('.', labels) : null;
    }

    /// <summary>
    /// Constrói uma resposta DNS com A record apontando para 127.0.0.1 (sinkhole).
    /// A resposta reutiliza o header e question da query original.
    /// </summary>
    private static byte[] BuildSinkholeResponse(byte[] query, string domain)
    {
        // Encontrar o fim da seção Question (QNAME + QTYPE + QCLASS)
        int qnameEnd = 12;
        while (qnameEnd < query.Length && query[qnameEnd] != 0) qnameEnd += query[qnameEnd] + 1;
        qnameEnd++; // pula o byte 0
        int questionEnd = qnameEnd + 4; // QTYPE (2) + QCLASS (2)

        // Response = Header + Question + Answer
        var response = new byte[questionEnd + 16]; // 16 bytes para o Answer record
        Array.Copy(query, 0, response, 0, Math.Min(questionEnd, response.Length));

        // Flags: QR=1 (response), AA=1 (authoritative), RA=1 (recursion available), RCODE=0 (no error)
        response[2] = 0x85; // QR=1, Opcode=0, AA=1, TC=0, RD=1
        response[3] = 0x80; // RA=1, Z=0, RCODE=0

        // QDCOUNT = 1
        response[4] = 0x00;
        response[5] = 0x01;

        // ANCOUNT = 1
        response[6] = 0x00;
        response[7] = 0x01;

        // NSCOUNT = 0, ARCOUNT = 0
        response[8] = 0; response[9] = 0;
        response[10] = 0; response[11] = 0;

        // Answer section (A record pointing to 127.0.0.1)
        int answerOffset = questionEnd;

        // NAME: pointer to QNAME in question section (0xC00C = offset 12)
        response[answerOffset] = 0xC0;
        response[answerOffset + 1] = 0x0C;

        // TYPE: A (1)
        response[answerOffset + 2] = 0x00;
        response[answerOffset + 3] = 0x01;

        // CLASS: IN (1)
        response[answerOffset + 4] = 0x00;
        response[answerOffset + 5] = 0x01;

        // TTL: 60 seconds (curto para que o sinkhole seja removível rapidamente)
        response[answerOffset + 6] = 0x00;
        response[answerOffset + 7] = 0x00;
        response[answerOffset + 8] = 0x00;
        response[answerOffset + 9] = 0x3C; // 60

        // RDLENGTH: 4 (IPv4)
        response[answerOffset + 10] = 0x00;
        response[answerOffset + 11] = 0x04;

        // RDATA: 127.0.0.1
        response[answerOffset + 12] = SinkholeIpBytes[0];
        response[answerOffset + 13] = SinkholeIpBytes[1];
        response[answerOffset + 14] = SinkholeIpBytes[2];
        response[answerOffset + 15] = SinkholeIpBytes[3];

        return response;
    }

    /// <summary>
    /// Encaminha a query DNS para o servidor upstream real e retorna a resposta ao cliente.
    /// </summary>
    private async Task ForwardToUpstream(byte[] queryData, IPEndPoint clientEndpoint, CancellationToken ct)
    {
        try
        {
            using var forwarder = new UdpClient();
            forwarder.Client.ReceiveTimeout = 3000; // 3 segundos timeout
            await forwarder.SendAsync(queryData, queryData.Length, _upstreamDns);

            var upstreamTask = forwarder.ReceiveAsync(ct);
            var timeoutTask = Task.Delay(3000, ct);
            var completed = await Task.WhenAny(upstreamTask.AsTask(), timeoutTask);

            if (completed == upstreamTask.AsTask() && upstreamTask.IsCompletedSuccessfully)
            {
                var upstreamResponse = upstreamTask.Result;
                await _listener!.SendAsync(
                    upstreamResponse.Buffer,
                    upstreamResponse.Buffer.Length,
                    clientEndpoint);
            }
            else
            {
                _logger.LogWarning("Timeout ao consultar DNS upstream para {Client}", clientEndpoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao encaminhar DNS para upstream");
        }
    }

    public void Stop()
    {
        _running = false;
        _listener?.Close();
        _logger.LogInformation("DNS Interceptor parado");
    }

    public void Dispose()
    {
        Stop();
        _listener?.Dispose();
    }
}

/// <summary>
/// Serviço auxiliar para reportar eventos de bloqueio (enviados ao servidor via SignalR).
/// </summary>
public class BlockEventReporter
{
    private readonly ILogger<BlockEventReporter> _logger;
    private readonly Queue<(string Domain, CompiledRuleDto? Rule, DateTime Timestamp)> _pendingEvents = new();
    private readonly object _queueLock = new();

    public BlockEventReporter(ILogger<BlockEventReporter> logger)
    {
        _logger = logger;
    }

    public void Report(string domain, CompiledRuleDto? rule)
    {
        lock (_queueLock)
        {
            _pendingEvents.Enqueue((domain, rule, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Retira todos os eventos pendentes da fila para envio ao servidor.
    /// </summary>
    public List<(string Domain, CompiledRuleDto? Rule, DateTime Timestamp)> DrainEvents()
    {
        lock (_queueLock)
        {
            var events = _pendingEvents.ToList();
            _pendingEvents.Clear();
            return events;
        }
    }
}

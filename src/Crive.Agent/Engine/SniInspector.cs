using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Engine;

public class SniInspector
{
    private readonly ILogger<SniInspector> _logger;
    private readonly RuleEvaluator _ruleEvaluator;
    private TcpListener? _tcpListener;
    private bool _running;

    public SniInspector(ILogger<SniInspector> logger, RuleEvaluator ruleEvaluator)
    {
        _logger = logger;
        _ruleEvaluator = ruleEvaluator;
    }

    public void Start()
    {
        _running = true;
        _tcpListener = new TcpListener(IPAddress.Loopback, 8443);
        _tcpListener.Start();
        Task.Run(ListenLoopAsync);
        _logger.LogInformation("SNI Inspector started on 127.0.0.1:8443");
    }

    private async Task ListenLoopAsync()
    {
        while (_running && _tcpListener != null)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                _ = ProcessClientAsync(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting SNI proxy connection");
            }
        }
    }

    private async Task ProcessClientAsync(TcpClient client)
    {
        // Parse TLS ClientHello, extract SNI, evaluate, proxy or drop
    }

    public void Stop()
    {
        _running = false;
        _tcpListener?.Stop();
    }
}

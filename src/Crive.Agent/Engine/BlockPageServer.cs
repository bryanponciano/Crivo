using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Engine;

/// <summary>
/// Servidor HTTP local que exibe a página de bloqueio quando um domínio é bloqueado.
/// 
/// Quando o DNS retorna 127.0.0.1 (sinkhole) para um domínio bloqueado,
/// o navegador tenta acessar http://127.0.0.1:80 — e este servidor responde
/// com uma página HTML profissional informando que o acesso foi bloqueado.
/// 
/// Para HTTPS (porta 443), o navegador receberá um erro de certificado SSL
/// (pois não temos certificado para o domínio bloqueado) — isso é aceitável
/// e evita a necessidade de instalar um certificado root CA corporativo.
/// </summary>
public sealed class BlockPageServer : IDisposable
{
    private readonly ILogger<BlockPageServer> _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly int _port;

    /// <summary>
    /// Página HTML de bloqueio com design profissional corporativo.
    /// Inclui: ícone de cadeado, mensagem clara, domínio bloqueado, timestamp.
    /// </summary>
    private const string BlockPageHtml = """
    <!DOCTYPE html>
    <html lang="pt-BR">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Acesso Bloqueado — Crivo</title>
        <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%);
                min-height: 100vh;
                display: flex;
                justify-content: center;
                align-items: center;
                padding: 20px;
            }

            .container {
                background: #ffffff;
                border-radius: 16px;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.4);
                max-width: 520px;
                width: 100%;
                overflow: hidden;
            }

            .header {
                background: linear-gradient(135deg, #1e3a5f 0%, #2563eb 100%);
                padding: 32px;
                text-align: center;
            }

            .shield-icon {
                width: 64px;
                height: 64px;
                margin: 0 auto 16px;
                background: rgba(255, 255, 255, 0.15);
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
            }

            .shield-icon svg {
                width: 32px;
                height: 32px;
                fill: none;
                stroke: white;
                stroke-width: 2;
                stroke-linecap: round;
                stroke-linejoin: round;
            }

            .header h1 {
                color: #ffffff;
                font-size: 22px;
                font-weight: 700;
                letter-spacing: -0.02em;
            }

            .header .subtitle {
                color: rgba(255, 255, 255, 0.75);
                font-size: 14px;
                margin-top: 6px;
            }

            .body {
                padding: 32px;
            }

            .message {
                color: #334155;
                font-size: 15px;
                line-height: 1.6;
                text-align: center;
                margin-bottom: 24px;
            }

            .domain-box {
                background: #f8fafc;
                border: 1px solid #e2e8f0;
                border-radius: 8px;
                padding: 16px;
                text-align: center;
                margin-bottom: 24px;
            }

            .domain-box .label {
                color: #94a3b8;
                font-size: 11px;
                text-transform: uppercase;
                letter-spacing: 0.08em;
                font-weight: 600;
                margin-bottom: 6px;
            }

            .domain-box .domain {
                color: #dc2626;
                font-size: 16px;
                font-weight: 600;
                font-family: 'SF Mono', 'Fira Code', 'Consolas', monospace;
                word-break: break-all;
            }

            .info {
                color: #64748b;
                font-size: 12px;
                text-align: center;
                line-height: 1.5;
            }

            .footer {
                border-top: 1px solid #f1f5f9;
                padding: 16px 32px;
                text-align: center;
            }

            .footer .brand {
                color: #94a3b8;
                font-size: 12px;
                font-weight: 500;
            }

            .footer .brand span {
                color: #2563eb;
                font-weight: 700;
            }
        </style>
    </head>
    <body>
        <div class="container">
            <div class="header">
                <div class="shield-icon">
                    <svg viewBox="0 0 24 24">
                        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                        <path d="M9 12l2 2 4-4" stroke-width="2.5"/>
                    </svg>
                </div>
                <h1>Acesso Bloqueado</h1>
                <div class="subtitle">Política de Segurança Corporativa</div>
            </div>
            <div class="body">
                <p class="message">
                    O acesso a este site foi restrito pelas políticas de segurança da sua organização.
                </p>
                <div class="domain-box">
                    <div class="label">Domínio Bloqueado</div>
                    <div class="domain">{{DOMAIN}}</div>
                </div>
                <p class="info">
                    Se você acredita que este bloqueio é um erro, entre em contato com o 
                    departamento de TI da sua empresa.
                </p>
            </div>
            <div class="footer">
                <div class="brand">Protegido por <span>Crivo</span></div>
            </div>
        </div>
    </body>
    </html>
    """;

    public BlockPageServer(ILogger<BlockPageServer> logger, int port = 8081)
    {
        _logger = logger;
        _port = port;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _listener.Prefixes.Add($"http://+:{_port}/"); // Aceita requisições de qualquer hostname (domínios sinkholeados)
        _listener.Start();
        _logger.LogInformation("Block Page Server iniciado em http://127.0.0.1:{Port}/", _port);
        return Task.Run(() => ListenLoopAsync(_cts.Token), ct);
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context), ct);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no Block Page Server");
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            // Extrair o domínio da requisição (o Host header contém o domínio bloqueado)
            var blockedDomain = context.Request.Url?.Host ?? context.Request.Headers["Host"] ?? "desconhecido";

            // Substituir o placeholder pelo domínio real
            var html = BlockPageHtml.Replace("{{DOMAIN}}", WebUtility.HtmlEncode(blockedDomain));
            var buffer = Encoding.UTF8.GetBytes(html);

            context.Response.StatusCode = 403;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.Headers.Add("X-Blocked-By", "Crivo");
            context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");

            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao servir block page");
            try { context.Response.Close(); } catch { }
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _logger.LogInformation("Block Page Server parado");
    }

    public void Dispose()
    {
        Stop();
        _listener?.Close();
        _cts?.Dispose();
    }
}

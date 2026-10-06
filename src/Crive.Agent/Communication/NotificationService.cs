using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Communication;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _lastNotified = new();

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowBlockNotification(string domain)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (_lastNotified.TryGetValue(domain, out var lastTime) && (now - lastTime).TotalSeconds < 10)
            {
                return; // Ignora se já notificou este domínio há menos de 10 segundos
            }
            _lastNotified[domain] = now;

            var title = "Crivo - Acesso Bloqueado";
            var message = $"O acesso ao site '{domain}' foi bloqueado pelas políticas da empresa.";

            // WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero
            // WTS_CURRENT_SESSION = -1 (mas queremos a sessão ativa do console, 1 ou 2)
            uint WTS_CURRENT_SESSION = unchecked((uint)-1);
            
            // Pega a sessão ativa
            var activeSessionId = WTSGetActiveConsoleSessionId();
            if (activeSessionId == 0xFFFFFFFF)
            {
                activeSessionId = 1; // Default fallback
            }

            WTSSendMessage(
                IntPtr.Zero, 
                activeSessionId, 
                title, (uint)title.Length * 2, 
                message, (uint)message.Length * 2, 
                0x00000030, // MB_ICONEXCLAMATION
                0, 
                out _, 
                false); // false = não bloqueia aguardando clique do usuário
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exibir notificação de bloqueio no Windows.");
        }
    }

    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool WTSSendMessage(
        IntPtr hServer,
        uint SessionId,
        string pTitle,
        uint TitleLength,
        string pMessage,
        uint MessageLength,
        uint Style,
        uint Timeout,
        out uint pResponse,
        bool bWait);

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();
}

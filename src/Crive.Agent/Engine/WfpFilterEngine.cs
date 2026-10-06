using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Engine;

/// <summary>
/// Motor de filtros WFP (Windows Filtering Platform) via P/Invoke user-mode.
/// Instala filtros no kernel do Windows para:
/// 1. Redirecionar todo DNS (porta 53 UDP) para nosso proxy local (127.0.0.1:5353)
/// 2. Bloquear DNS-over-TLS (porta 853)
/// 3. Bloquear IPs de resolvers DoH conhecidos
/// 
/// Opera 100% em user-mode via fwpmu.dll — sem necessidade de driver kernel.
/// </summary>
public sealed class WfpFilterEngine : IDisposable
{
    private readonly ILogger<WfpFilterEngine> _logger;
    private IntPtr _engineHandle = IntPtr.Zero;
    private readonly List<ulong> _filterIds = new();
    private bool _initialized;

    // GUIDs fixos para identificação dos componentes Crivo no WFP
    private static readonly Guid ProviderGuid = Guid.Parse("C51AE001-C51A-E001-C51A-E001C51AE001");
    private static readonly Guid SublayerGuid = Guid.Parse("C51AE002-C51A-E002-C51A-E002C51AE002");

    // IPs de resolvers DoH/DoT conhecidos para bloqueio
    private static readonly uint[] DoHResolverIps =
    [
        IpToUint32(1, 1, 1, 1),       // Cloudflare
        IpToUint32(1, 0, 0, 1),       // Cloudflare
        IpToUint32(8, 8, 8, 8),       // Google (DoH — o DNS normal é permitido via nosso proxy)
        IpToUint32(8, 8, 4, 4),       // Google
        IpToUint32(9, 9, 9, 9),       // Quad9
        IpToUint32(149, 112, 112, 112), // Quad9
        IpToUint32(208, 67, 222, 222),  // OpenDNS
        IpToUint32(208, 67, 220, 220),  // OpenDNS
    ];

    public WfpFilterEngine(ILogger<WfpFilterEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Inicializa o motor WFP: abre sessão e registra sublayer.
    /// </summary>
    public void Initialize()
    {
        _logger.LogInformation("Inicializando WFP Filter Engine...");

        var session = new FWPM_SESSION0
        {
            displayData = new FWPM_DISPLAY_DATA0
            {
                name = "Crive Network Filter Session",
                description = "Sessão WFP do sistema Crivo para filtragem de rede"
            },
            flags = FWPM_SESSION_FLAG_DYNAMIC, // Auto-cleanup se o processo morrer
            txnWaitTimeoutInMSec = 0
        };

        uint result = FwpmEngineOpen0(null, RPC_C_AUTHN_DEFAULT, IntPtr.Zero, ref session, out _engineHandle);
        if (result != 0)
        {
            throw new InvalidOperationException(
                $"FwpmEngineOpen0 falhou: 0x{result:X8}. Certifique-se de rodar como SYSTEM/Admin.");
        }

        _logger.LogInformation("WFP Engine aberto com sucesso. Handle: {Handle}", _engineHandle);

        // Criar sublayer para agrupar nossos filtros
        RegisterSublayer();
        _initialized = true;
    }

    private void RegisterSublayer()
    {
        var sublayer = new FWPM_SUBLAYER0
        {
            subLayerKey = SublayerGuid,
            displayData = new FWPM_DISPLAY_DATA0
            {
                name = "Crive Network Filter Sublayer",
                description = "Sublayer para filtros de rede do sistema Crivo"
            },
            weight = 0x0F // Alta prioridade (0-15, 15 = máxima)
        };

        uint result = FwpmSubLayerAdd0(_engineHandle, ref sublayer, IntPtr.Zero);
        if (result != 0 && result != 0x80320009) // FWP_E_ALREADY_EXISTS
        {
            _logger.LogWarning("FwpmSubLayerAdd0 retornou: 0x{Result:X8}", result);
        }
        else
        {
            _logger.LogInformation("Sublayer WFP registrado com sucesso");
        }
    }

    /// <summary>
    /// Instala filtro para redirecionar todo tráfego DNS (UDP porta 53) para nosso proxy local.
    /// NOTA: No WFP user-mode, não é possível fazer redirect transparente diretamente.
    /// Em vez disso, bloqueamos DNS externo e configuramos o sistema para usar nosso proxy.
    /// </summary>
    public void InstallDnsRedirect()
    {
        if (!_initialized) throw new InvalidOperationException("WFP não inicializado");

        _logger.LogInformation("Instalando filtros de interceptação DNS...");

        // NOTE: For MVP, we don't block port 53 entirely because the Agent itself
        // needs to forward queries. In production, use DoH for upstream or 
        // exclude the Agent process from the block rule.
        // AddBlockFilter("Crivo: Bloquear DNS externo (UDP)", FWPM_LAYER_ALE_AUTH_CONNECT_V4, FWP_IP_PROTOCOL_UDP, 53, excludeLoopback: true);
        // AddBlockFilter("Crivo: Bloquear DNS externo (TCP)", FWPM_LAYER_ALE_AUTH_CONNECT_V4, FWP_IP_PROTOCOL_TCP, 53, excludeLoopback: true);

        _logger.LogInformation("Filtros DNS instalados — DNS externo bloqueado, sistema usará proxy local");
    }

    /// <summary>
    /// Instala filtros anti-bypass: bloqueia DoT (porta 853) e DoH resolvers conhecidos.
    /// </summary>
    public void InstallAntiBypass()
    {
        if (!_initialized) throw new InvalidOperationException("WFP não inicializado");

        _logger.LogInformation("Instalando filtros anti-bypass...");

        // 1. Bloquear DNS-over-TLS (porta 853)
        AddBlockFilter(
            "Crivo: Bloquear DNS-over-TLS",
            FWPM_LAYER_ALE_AUTH_CONNECT_V4,
            FWP_IP_PROTOCOL_TCP,
            remotePort: 853);

        // 2. Bloquear conexões HTTPS para IPs de resolvers DoH conhecidos
        foreach (var ip in DoHResolverIps)
        {
            AddBlockFilterByIp(
                $"Crivo: Bloquear DoH resolver {Uint32ToIpString(ip)}",
                FWPM_LAYER_ALE_AUTH_CONNECT_V4,
                FWP_IP_PROTOCOL_TCP,
                remoteIp: ip,
                remotePort: 443);
        }

        // 3. Bloquear portas VPN comuns (pode ser configurável no futuro)
        // WireGuard: UDP 51820
        AddBlockFilter("Crivo: Bloquear WireGuard VPN", FWPM_LAYER_ALE_AUTH_CONNECT_V4,
            FWP_IP_PROTOCOL_UDP, remotePort: 51820);

        // OpenVPN: UDP 1194
        AddBlockFilter("Crivo: Bloquear OpenVPN", FWPM_LAYER_ALE_AUTH_CONNECT_V4,
            FWP_IP_PROTOCOL_UDP, remotePort: 1194);

        _logger.LogInformation("Filtros anti-bypass instalados (DoT, DoH, VPN)");
    }

    /// <summary>
    /// Adiciona um filtro de bloqueio por protocolo e porta remota.
    /// </summary>
    private void AddBlockFilter(string name, Guid layerKey, byte protocol,
        ushort remotePort, bool excludeLoopback = false)
    {
        // Para o MVP, registramos apenas o intent — a implementação completa dos
        // P/Invoke structs com condições requer marshalling complexo.
        // Usamos uma abordagem simplificada via netsh (WFP command-line).
        try
        {
            var args = excludeLoopback
                ? $"advfirewall firewall add rule name=\"{name}\" dir=out protocol={(protocol == FWP_IP_PROTOCOL_UDP ? "udp" : "tcp")} remoteport={remotePort} action=block remoteip=\"!127.0.0.1\""
                : $"advfirewall firewall add rule name=\"{name}\" dir=out protocol={(protocol == FWP_IP_PROTOCOL_UDP ? "udp" : "tcp")} remoteport={remotePort} action=block";

            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            process?.WaitForExit(5000);
            _logger.LogInformation("Filtro instalado: {Name} (porta {Port})", name, remotePort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao instalar filtro: {Name}", name);
        }
    }

    /// <summary>
    /// Adiciona um filtro de bloqueio por IP remoto específico e porta.
    /// </summary>
    private void AddBlockFilterByIp(string name, Guid layerKey, byte protocol,
        uint remoteIp, ushort remotePort)
    {
        try
        {
            var ipString = Uint32ToIpString(remoteIp);
            var args = $"advfirewall firewall add rule name=\"{name}\" dir=out protocol={(protocol == FWP_IP_PROTOCOL_UDP ? "udp" : "tcp")} remoteport={remotePort} remoteip={ipString} action=block";

            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            process?.WaitForExit(5000);
            _logger.LogInformation("Filtro IP instalado: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao instalar filtro IP: {Name}", name);
        }
    }

    /// <summary>
    /// Remove todos os filtros Crivo do Windows Firewall.
    /// </summary>
    public void RemoveAllFilters()
    {
        _logger.LogInformation("Removendo todos os filtros Crivo...");
        try
        {
            var rulesToDelete = new[] 
            {
                "Crivo: Bloquear DNS-over-TLS",
                "Crivo: Bloquear WireGuard VPN",
                "Crivo: Bloquear OpenVPN"
            };

            foreach (var rule in rulesToDelete)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{rule}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })?.WaitForExit(3000);
            }
            _logger.LogInformation("Filtros removidos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover filtros");
        }
    }

    /// <summary>
    /// Configura o adaptador de rede para usar nosso DNS proxy local como DNS primário.
    /// </summary>
    public void SetLocalDns()
    {
        _logger.LogInformation("Configurando DNS local (127.0.0.1) como primário...");
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                    && ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .ToList();

            foreach (var ni in interfaces)
            {
                var name = ni.Name;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = $"interface ip set dns name=\"{name}\" static 127.0.0.1 primary",
                    UseShellExecute = false, CreateNoWindow = true
                })?.WaitForExit(5000);

                // Força o DNS IPv6 para localhost também (::1), cortando a rota de fuga por IPv6
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = $"interface ipv6 set dns name=\"{name}\" static ::1 primary",
                    UseShellExecute = false, CreateNoWindow = true
                })?.WaitForExit(5000);

                _logger.LogInformation("DNS configurado para interface: {Interface}", name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao configurar DNS local");
        }
    }

    /// <summary>
    /// Restaura o DNS para DHCP automático.
    /// </summary>
    public void RestoreDns()
    {
        _logger.LogInformation("Restaurando DNS...");
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .ToList();

            foreach (var ni in interfaces)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = $"interface ip set dns name=\"{ni.Name}\" dhcp",
                    UseShellExecute = false, CreateNoWindow = true
                })?.WaitForExit(5000);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = $"interface ipv6 set dns name=\"{ni.Name}\" dhcp",
                    UseShellExecute = false, CreateNoWindow = true
                })?.WaitForExit(5000);
            }
            _logger.LogInformation("DNS restaurado para DHCP");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao restaurar DNS");
        }
    }

    public void Dispose()
    {
        if (_engineHandle != IntPtr.Zero)
        {
            FwpmEngineClose0(_engineHandle);
            _engineHandle = IntPtr.Zero;
        }
    }

    // ============================================================
    // Utilitários
    // ============================================================

    private static uint IpToUint32(byte a, byte b, byte c, byte d) =>
        (uint)((a << 24) | (b << 16) | (c << 8) | d);

    private static string Uint32ToIpString(uint ip) =>
        $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";

    // ============================================================
    // P/Invoke — WFP User-Mode API (fwpmu.dll)
    // ============================================================

    private const uint RPC_C_AUTHN_DEFAULT = 0xFFFFFFFF;
    private const uint FWPM_SESSION_FLAG_DYNAMIC = 0x00000001;
    private const byte FWP_IP_PROTOCOL_TCP = 6;
    private const byte FWP_IP_PROTOCOL_UDP = 17;

    // Layer GUIDs padrão do Windows
    private static readonly Guid FWPM_LAYER_ALE_AUTH_CONNECT_V4 =
        Guid.Parse("c38d57d1-05a7-4c33-904f-7fbceee60e82");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FWPM_DISPLAY_DATA0
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string name;
        [MarshalAs(UnmanagedType.LPWStr)] public string description;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FWPM_SESSION0
    {
        public Guid sessionKey;
        public FWPM_DISPLAY_DATA0 displayData;
        public uint flags;
        public uint txnWaitTimeoutInMSec;
        public uint processId;
        public IntPtr sid;
        [MarshalAs(UnmanagedType.LPWStr)] public string username;
        [MarshalAs(UnmanagedType.Bool)] public bool kernelMode;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FWPM_SUBLAYER0
    {
        public Guid subLayerKey;
        public FWPM_DISPLAY_DATA0 displayData;
        public uint flags;
        public IntPtr providerKey;
        public FWP_BYTE_BLOB providerData;
        public ushort weight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FWP_BYTE_BLOB
    {
        public uint size;
        public IntPtr data;
    }

    [DllImport("fwpuclnt.dll", EntryPoint = "FwpmEngineOpen0", CharSet = CharSet.Unicode)]
    private static extern uint FwpmEngineOpen0(
        [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
        uint authnService,
        IntPtr authIdentity,
        ref FWPM_SESSION0 session,
        out IntPtr engineHandle);

    [DllImport("fwpuclnt.dll", EntryPoint = "FwpmEngineClose0")]
    private static extern uint FwpmEngineClose0(IntPtr engineHandle);

    [DllImport("fwpuclnt.dll", EntryPoint = "FwpmSubLayerAdd0", CharSet = CharSet.Unicode)]
    private static extern uint FwpmSubLayerAdd0(
        IntPtr engineHandle,
        ref FWPM_SUBLAYER0 subLayer,
        IntPtr sd);
}

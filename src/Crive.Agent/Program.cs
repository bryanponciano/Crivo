using Crive.Agent.Communication;
using Crive.Agent.Engine;
using Crive.Agent.Protection;
using Crive.Agent.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crive.Agent;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // First run check
        if (!OfflineCache.ConfigExists())
        {
            // Tenta restaurar o DNS via netsh caso o agente tenha crashado antes
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = "interface ip set dns name=\"Wi-Fi\" dhcp",
                    CreateNoWindow = true, UseShellExecute = false
                })?.WaitForExit(2000);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = "interface ipv6 set dns name=\"Wi-Fi\" dhcp",
                    CreateNoWindow = true, UseShellExecute = false
                })?.WaitForExit(2000);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = "interface ip set dns name=\"Ethernet\" dhcp",
                    CreateNoWindow = true, UseShellExecute = false
                })?.WaitForExit(2000);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "netsh", Arguments = "interface ipv6 set dns name=\"Ethernet\" dhcp",
                    CreateNoWindow = true, UseShellExecute = false
                })?.WaitForExit(2000);
            } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var formHost = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<SetupForm>();
                    services.AddSingleton<OfflineCache>();
                    services.AddSingleton<EnrollmentService>();
                    services.AddSingleton<Crive.Agent.Communication.NotificationService>();
                    services.AddHttpClient();
                }).Build();
                
            Application.Run(formHost.Services.GetRequiredService<SetupForm>());
        }

        if (!OfflineCache.ConfigExists())
        {
            // Se ainda não estiver configurado após o form fechar, sai do programa
            return;
        }

        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "CriveAgent";
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<OfflineCache>();
                services.AddSingleton<RuleEvaluator>();
                services.AddSingleton<WfpFilterEngine>();
                services.AddSingleton<Crive.Agent.Communication.NotificationService>();
                services.AddSingleton<DnsInterceptor>();
                services.AddSingleton<BlockPageServer>();
                services.AddSingleton<CriveSignalRClient>();
                services.AddSingleton<BlockEventReporter>();
                services.AddSingleton<SelfProtection>();

                services.AddHostedService<Worker>();
            })
            .Build();

        host.Run();
    }
}

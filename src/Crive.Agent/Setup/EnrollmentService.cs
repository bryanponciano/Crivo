using System.Net.Http.Json;
using Crive.Agent.Communication;
using Crive.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Setup;

public class EnrollmentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(HttpClient httpClient, ILogger<EnrollmentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SectorListItemDto>> GetSectorsAsync(string serverUrl, string tenantCode)
    {
        try
        {
            var url = $"{serverUrl.TrimEnd('/')}/api/setup/sectors?tenantCode={tenantCode}";
            return await _httpClient.GetFromJsonAsync<List<SectorListItemDto>>(url) ?? new List<SectorListItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sectors.");
            return new List<SectorListItemDto>();
        }
    }

    public async Task<AgentConfig?> RegisterAsync(string serverUrl, string tenantCode, Guid sectorId, string employee, string assetNumber)
    {
        try
        {
            var url = $"{serverUrl.TrimEnd('/')}/api/setup/register";
            var request = new MachineRegistrationDto
            {
                TenantCode = tenantCode,
                SectorId = sectorId,
                EmployeeName = employee,
                AssetNumber = assetNumber,
                Hostname = Environment.MachineName,
                HardwareFingerprint = GetHardwareFingerprint(),
                AgentVersion = "1.0.0"
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MachineRegistrationResponseDto>();
                if (result != null)
                {
                    return new AgentConfig
                    {
                        ServerUrl = serverUrl,
                        DeviceToken = result.DeviceToken,
                        MachineId = result.MachineId,
                        TenantCode = tenantCode
                    };
                }
            }
            
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Registration failed: {Error}", err);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration exception");
            return null;
        }
    }

    private string GetHardwareFingerprint()
    {
        // Simplistic fingerprint for MVP
        return Environment.MachineName + "-" + Environment.UserName;
    }
}

using System.Net;
using CoreNode.Domain.DTOs;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreNode.Infrastructure.Services;

public class ProxmoxApiService : IProxmoxApiService
{
    private readonly HttpClient _httpClient;
    private readonly ProxmoxOptions _options;
    private readonly ILogger<ProxmoxApiService> _logger;
    
    public ProxmoxApiService(HttpClient httpClient, IOptions<ProxmoxOptions> options, ILogger<ProxmoxApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"PVEAPIToken={_options.TokenId}={_options.Secret}");
    }
    
    public async Task<string> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
       _logger.LogInformation("Getting Proxmox status.....");
       
       var response = await _httpClient.GetAsync("cluster/status", cancellationToken);
       
       response.EnsureSuccessStatusCode();
       
       return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<int> GetNextVmidAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("cluster/next", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var nextIdString = await response.Content.ReadAsStringAsync(cancellationToken);
        
        return int.Parse(nextIdString);
        
    }
    public async Task<string> CreateLxcContainerAsync(CreateLxcRequest request, CancellationToken cancellationToken = default)
    {
        var nextVmId = await GetNextVmidAsync(cancellationToken);
        var proxmoxData = new Dictionary<string, string>
        {
            { "vmid",nextVmId.ToString() },
            { "hostname", request.Hostname },
            { "ostemplate", request.OsTemplate },
            { "password", request.Password },
            { "memory", request.MemoryMB.ToString() },
            { "cores", request.Cores.ToString() },
        };
        var content = new FormUrlEncodedContent(proxmoxData);
        
        var response =await  _httpClient.PostAsync("nodes/pve/lxc",content,cancellationToken);

        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
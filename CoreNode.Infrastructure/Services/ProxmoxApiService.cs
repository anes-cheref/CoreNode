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
        
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization", 
            $"PVEAPIToken={_options.TokenId}={_options.Secret}");    }
    
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
        /*
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
        */
        _logger.LogInformation("SIMULATION : Fausse création de la VM {Hostname}", request.Hostname);
    
        // On simule le temps de réponse d'un serveur (1 seconde d'attente)
        await Task.Delay(1000, cancellationToken);
    
        // On retourne un faux UPID au format Proxmox
        return $"UPID:pve:00000000:00000000:{Guid.NewGuid():N}:vzdump:100:root@pam:";
    }
}
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

    public Task<string> CreateLxcContainerAsync(CreateLxcRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
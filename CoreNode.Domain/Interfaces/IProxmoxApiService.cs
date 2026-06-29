using CoreNode.Domain.DTOs;

namespace CoreNode.Domain.Interfaces;

public interface IProxmoxApiService
{
    Task<string> GetClusterStatusAsync(CancellationToken cancellationToken = default);
    
    // La méthode prend bien notre DTO en paramètre
    Task<string> CreateLxcContainerAsync(CreateLxcRequest request, CancellationToken cancellationToken = default);
}
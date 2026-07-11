using CoreNode.Domain.DTOs;

namespace CoreNode.Domain.Interfaces;

public interface IProxmoxApiService
{
    Task<string> GetClusterStatusAsync(CancellationToken cancellationToken = default);
    Task<string> CreateLxcContainerAsync(CreateLxcRequest request, CancellationToken cancellationToken = default);
    Task<string> GetTaskStatusAsync(string node, string upid, CancellationToken cancellationToken = default);
    Task<string> DeleteLxcContainerAsync(Guid vmId,CancellationToken cancellationToken = default);
}
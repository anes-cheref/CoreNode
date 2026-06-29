using CoreNode.Domain.Enums;

namespace CoreNode.Domain.Entities;

public class VirtualMachine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public int? ProxmoxVmId { get; set; }
    
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    
    public string Hostname { get; set; } = string.Empty;
    public int MemoryMB { get; set; }
    public VmStatus Status { get; set; } = VmStatus.Pending;
    public string? Ipv4Address { get; set; }
    
    public ICollection<ProxmoxTask> ProxmoxTask { get; set; } = new List<ProxmoxTask>();
}
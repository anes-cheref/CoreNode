using CoreNode.Domain.Enums;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace CoreNode.Domain.Entities;

public class ProxmoxTask
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    
    public string Upid { get; set; } = string.Empty;
    
    public Guid VirtualMachineId { get; set; }
    public VirtualMachine? VirtualMachine { get; set; }
    
    public TaskType Type { get; set; }
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.InProgress;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
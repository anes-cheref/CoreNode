using System.Runtime.InteropServices.Marshalling;

namespace CoreNode.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    public string Email { get; set; } =  string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VirtualMachine> VirtualMachines { get; set; } = new List<VirtualMachine>();
}
namespace CoreNode.Infrastructure.Configuration;

public class ProxmoxOptions
{
    public const string SectionName = "Proxmox";
    
    public string BaseUrl { get; set; }
    public string TokenId { get; set; }
    public string Secret { get; set; }
}
namespace CoreNode.Domain.DTOs;

public class CreateLxcRequest
{
    public string Hostname { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MemoryMB { get; set; } = 512;
    public int Cores { get; set; } = 1;
    public string OsTemplate { get; set; } = "local:vztmpl/ubuntu-22.04-standard_22.04-1_amd64.tar.zst";
}
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryInHours { get; set; }
}
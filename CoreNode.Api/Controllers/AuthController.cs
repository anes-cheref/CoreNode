using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreNode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;

    // L'injection de dépendance nous donne un IOptions, mais on extrait directement 
    // la .Value pour ne pas alourdir le code en dessous.
    public AuthController(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public IActionResult Login()
    {
        // 1. On triche pour l'instant : on génère un token pour l'ID de la ligne que tu avais créée dans DBeaver
        var tenantId = "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"; // Remplace par ton vrai ID si besoin

        // 2. On récupère la clé proprement via la configuration
        var secretKey = _jwtSettings.SecretKey;
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        // 3. On crée la "carte d'identité" (Claims) qui sera chiffrée dans le Token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tenantId),
            new Claim(JwtRegisteredClaimNames.Email, "anes@corenode")
        };

        // 4. On paramètre le Token en utilisant l'expiration dynamique
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryInHours),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        // 5. On génère le Token final
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new { Token = tokenHandler.WriteToken(token) });
    }
}
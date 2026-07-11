using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Configuration;
using CoreNode.Infrastructure.Data;
using CoreNode.Infrastructure.Services;
using CoreNode.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION DES SERVICES (Le Container) ---

// NOUVEAU : On déclare les contrôleurs au système
builder.Services.AddControllers(); 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insère ton JWT généré par la route /login ici. Attention : écris 'Bearer ' suivi d'un espace avant ton token ! Exemple : 'Bearer eyJhb...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<CoreNodeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ProxmoxOptions>(builder.Configuration.GetSection(ProxmoxOptions.SectionName));
builder.Services.AddHostedService<ProxmoxTaskWorker>();

builder.Services.AddHttpClient<IProxmoxApiService, ProxmoxApiService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    });

// 1. La clé maître (À terme, on la cachera dans appsettings.json)
var secretKey = "MaSuperCleSecreteQuiDoitFaireAuMoins32CaracteresPourEtreValide!"; 
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

// 2. L'embauche de l'agent de sécurité JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // On ne vérifie pas qui l'a émis pour l'instant
            ValidateAudience = false, // On ne vérifie pas pour qui c'est prévu
            ValidateLifetime = true, // On vérifie que le token n'est pas expiré
            ValidateIssuerSigningKey = true, // On vérifie la signature cryptographique
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

// 3. Activation du système d'autorisation global
builder.Services.AddAuthorization();

var app = builder.Build();

// --- 2. CONFIGURATION DU PIPELINE HTTP (Le flux des requêtes) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // "Qui es-tu ?" (Vérifie le Token)
app.UseAuthorization();  // "As-tu le droit de faire ça ?" (Vérifie les permissions)

// On active le routage vers les contrôleurs
app.MapControllers(); 

app.Run();
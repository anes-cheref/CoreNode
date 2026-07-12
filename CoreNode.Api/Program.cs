using System.Text;
using CoreNode.Api.Middlewares;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Data;
using CoreNode.Infrastructure.Services;
using CoreNode.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// 1. Initialisation de Serilog (La Boîte Noire)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/corenode-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de l'API CoreNode...");
    var builder = WebApplication.CreateBuilder(args);

    // 2. Remplacement du logger .NET par Serilog
    builder.Host.UseSerilog();

    // --- CONFIGURATION DES SERVICES ---

    builder.Services.AddControllers();

    // Base de données
    builder.Services.AddDbContext<CoreNodeDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Injection des dépendances (Services & Worker)
    builder.Services.AddScoped<IProxmoxApiService, ProxmoxApiService>();
    builder.Services.AddHostedService<ProxmoxTaskWorker>();

    // Authentification JWT
    var secretKey = "MaSuperCleSecreteQuiDoitFaireAuMoins32CaracteresPourEtreValide!";
    var keyBytes = Encoding.UTF8.GetBytes(secretKey);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
            };
        });

    builder.Services.AddAuthorization();

    // Swagger avec support du Token JWT
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Format attendu : 'Bearer {token}'",
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

    // --- CONSTRUCTION DE L'APPLICATION ---
    var app = builder.Build();

    // 3. Activation du filet de sécurité (Middleware)
    // Il doit être placé très tôt pour attraper les erreurs des couches suivantes
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // 4. Les portiques de sécurité
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application a crashé au démarrage");
}
finally
{
    Log.CloseAndFlush();
}
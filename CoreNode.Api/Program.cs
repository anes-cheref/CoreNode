using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Configuration;
using CoreNode.Infrastructure.Data;
using CoreNode.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION DES SERVICES (Le Container) ---

// NOUVEAU : On déclare les contrôleurs au système
builder.Services.AddControllers(); 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CoreNodeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ProxmoxOptions>(builder.Configuration.GetSection(ProxmoxOptions.SectionName));

builder.Services.AddHttpClient<IProxmoxApiService, ProxmoxApiService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    });

var app = builder.Build();

// --- 2. CONFIGURATION DU PIPELINE HTTP (Le flux des requêtes) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// NOUVEAU : On active le routage vers tes contrôleurs
app.MapControllers(); 

app.Run();
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Configuration;
using CoreNode.Infrastructure.Data;
using CoreNode.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CoreNodeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ProxmoxOptions>(builder.Configuration.GetSection(ProxmoxOptions.SectionName));

builder.Services.AddHttpClient<IProxmoxApiService, ProxmoxApiService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // ATTENTION : À laisser uniquement si ton Proxmox chez OVH utilise un certificat auto-signé
        // Sinon, le HttpClient refusera de s'y connecter par sécurité.
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();


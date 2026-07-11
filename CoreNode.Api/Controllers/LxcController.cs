using System.Security.Claims;
using CoreNode.Domain.DTOs;
using CoreNode.Domain.Entities;
using CoreNode.Domain.Enums;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatus = CoreNode.Domain.Enums.TaskStatus;

namespace CoreNode.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LxcController : ControllerBase
{
    private readonly CoreNodeDbContext _dbContext;
    private readonly IProxmoxApiService _proxmoxApiService;
    
    public LxcController(CoreNodeDbContext dbContext, IProxmoxApiService proxmoxApiService)
    {
        _dbContext = dbContext;
        _proxmoxApiService = proxmoxApiService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateLxcAsync(
        [FromBody] CreateLxcRequest lxcRequest, 
        CancellationToken cancellationToken = default)
    {
        // 1. On récupère le vrai utilisateur (la ligne que tu as créée dans DBeaver)
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        // 2. MAPPING avec le vrai TenantId
        var vm = new VirtualMachine
        {
            TenantId = tenantId, // <-- Le lien sécurisé avec la clé étrangère est ici
            Hostname = lxcRequest.Hostname,
            MemoryMB = lxcRequest.MemoryMB,
            Status = VmStatus.Creating 
        };
        
        _dbContext.VirtualMachines.Add(vm);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // 3. Appel à Proxmox (Mocké)
        var upid = await _proxmoxApiService.CreateLxcContainerAsync(lxcRequest, cancellationToken);
        
        // 4. Suivi de la tâche
        var proxmoxTask = new ProxmoxTask
        {
            Upid = upid,
            VirtualMachineId = vm.Id,
            Type = TaskType.Creation,
            Status = TaskStatus.InProgress
        };
        
        _dbContext.Tasks.Add(proxmoxTask);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. La réponse
        return Ok(new { Message = "Création lancée avec succès", VmId = vm.Id, Upid = upid });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVirtualMachinesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var myMachines = await _dbContext.VirtualMachines.Where(vm => vm.TenantId == tenantId).ToListAsync(cancellationToken);
        
        return Ok(myMachines);
        
    }
}
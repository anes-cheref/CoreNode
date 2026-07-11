using System.Security.Claims;
using CoreNode.Domain.DTOs;
using CoreNode.Domain.Entities;
using CoreNode.Domain.Enums;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Data;
using CoreNode.Infrastructure.Workers;
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLxcAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var vm = await  _dbContext.VirtualMachines.FirstOrDefaultAsync(v=> v.Id == id,cancellationToken);

        if (vm == null || tenantId != vm.TenantId)
        {
            return Forbid();
        }   
        else
        {
            vm.Status = VmStatus.Deleting;
            var upid = await _proxmoxApiService.DeleteLxcContainerAsync(vm.Id, cancellationToken);

            var ProxmoxTask = new ProxmoxTask
            {
                Upid = upid,
                VirtualMachineId = vm.Id,
                Type = TaskType.Deletion,
                Status = TaskStatus.InProgress
            };
            _dbContext.Tasks.Add(ProxmoxTask);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return Ok(new {Message = "Suppression lancée avec succès", VmId = vm.Id, Upid = upid });
        }
        
    }
}
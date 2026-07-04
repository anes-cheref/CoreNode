using CoreNode.Domain.DTOs;
using CoreNode.Domain.Entities;
using CoreNode.Domain.Enums;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = CoreNode.Domain.Enums.TaskStatus;

namespace CoreNode.Api.Controllers;

[ApiController]
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
        
        var vm = new VirtualMachine
        {
            // On simule un utilisateur pour l'instant (sera remplacé par le Token JWT plus tard)
            TenantId = Guid.NewGuid(), 
            Hostname = lxcRequest.Hostname,
            MemoryMB = lxcRequest.MemoryMB,
            Status = VmStatus.Creating 
        };
        
        _dbContext.VirtualMachines.Add(vm);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        var upid = await _proxmoxApiService.CreateLxcContainerAsync(lxcRequest, cancellationToken);
        
        var proxmoxTask = new ProxmoxTask
        {
            Upid = upid,
            VirtualMachineId = vm.Id,
            Type = TaskType.Creation,
            Status = TaskStatus.InProgress
        };
        
        _dbContext.Tasks.Add(proxmoxTask);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. LA RÉPONSE : On retourne un 200 OK avec l'ID de la machine créée pour le front-end
        return Ok(new { Message = "Création lancée avec succès", VmId = vm.Id, Upid = upid });
    }
}
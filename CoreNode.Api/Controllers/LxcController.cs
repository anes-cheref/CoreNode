using System.Security.Claims;
using CoreNode.Domain.DTOs;
using CoreNode.Domain.Entities;
using CoreNode.Domain.Enums;
using CoreNode.Domain.Exceptions; // <-- N'oublie pas de créer ce dossier/namespace dans ton projet Domain
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
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        // --- 🛑 RÈGLE BUSINESS : VÉRIFICATION DU QUOTA ---
        var currentMachineCount = await _dbContext.VirtualMachines
            .CountAsync(vm => vm.TenantId == tenantId, cancellationToken);

        if (currentMachineCount >= 3)
        {
            // On jette l'exception métier. Le Middleware s'occupera de la transformer en Erreur 400.
            throw new QuotaExceededException("Quota dépassé : Vous avez atteint la limite maximale de 3 machines virtuelles.");
        }
        // ------------------------------------------------
        
        var vm = new VirtualMachine
        {
            TenantId = tenantId,
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

        return Ok(new { Message = "Création lancée avec succès", VmId = vm.Id, Upid = upid });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVirtualMachinesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var myMachines = await _dbContext.VirtualMachines
            .Where(vm => vm.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        
        return Ok(myMachines);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLxcAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var vm = await _dbContext.VirtualMachines
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vm == null || vm.TenantId != tenantId)
        {
            return NotFound(); 
        }

        vm.Status = VmStatus.Deleting;

        var upid = await _proxmoxApiService.DeleteLxcContainerAsync(id, cancellationToken);

        var proxmoxTask = new ProxmoxTask
        {
            Upid = upid,
            VirtualMachineId = vm.Id,
            Type = TaskType.Deletion, 
            Status = TaskStatus.InProgress
        };

        _dbContext.Tasks.Add(proxmoxTask);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = "Suppression lancée avec succès", VmId = vm.Id, Upid = upid });
    }
}
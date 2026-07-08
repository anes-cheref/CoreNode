using CoreNode.Domain.Enums;
using CoreNode.Domain.Interfaces;
using CoreNode.Infrastructure.Data;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskStatus = CoreNode.Domain.Enums.TaskStatus;

namespace CoreNode.Infrastructure.Workers;

public class ProxmoxTaskWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProxmoxTaskWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CoreNodeDbContext>();
                var proxmoxService = scope.ServiceProvider.GetRequiredService<IProxmoxApiService>();
                
                // 1. On inclut la machine virtuelle et on exécute la requête en asynchrone
                var tasksInProgress = await dbContext.Tasks
                    .Include(t => t.VirtualMachine) 
                    .Where(t => t.Status == TaskStatus.InProgress)
                    .ToListAsync(stoppingToken);

                if (tasksInProgress.Count > 0)
                {
                    foreach (var task in tasksInProgress)
                    {
                        Console.WriteLine($"Vérification de la tâche {task.Id} pour la VM {task.VirtualMachine.Hostname}....");
                        var status = await proxmoxService.GetTaskStatusAsync("pve", task.Upid,stoppingToken);

                        if (status.Equals("stopped"))
                        {
                            task.Status = TaskStatus.Completed;
                            task.VirtualMachine.Status = VmStatus.Running;
                        }
                        else
                            Console.WriteLine("Tâche toujours en cours...");
                            
                    }
                    
                    
                    await dbContext.SaveChangesAsync(stoppingToken);
                    Console.WriteLine($"{tasksInProgress.Count} tâches mises à jour en base !");
                }
            }
            await Task.Delay(10000, stoppingToken);
        }
    }
}
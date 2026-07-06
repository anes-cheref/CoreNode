using System.ComponentModel;
using CoreNode.Infrastructure.Data;
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
                
                var tasksInProgress = dbContext.Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();
                
                Console.WriteLine("J'ai trouvé les tâches in progress. Il y en a : " + tasksInProgress.Count);
            }
           
           await Task.Delay(10000,stoppingToken);
        }
    }
}
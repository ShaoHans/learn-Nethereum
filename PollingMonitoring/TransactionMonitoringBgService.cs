using Microsoft.Extensions.Hosting;

namespace PollingMonitoring;

internal class TransactionMonitoringBgService : BackgroundService
{
    public TransactionMonitoringBgService()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
    }
}

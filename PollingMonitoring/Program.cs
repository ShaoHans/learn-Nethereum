using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PollingMonitoring;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(
        (context, config) =>
        {
            config
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>();
        }
    )
    .ConfigureServices(services =>
    {
        services.AddHostedService<TransactionMonitoringBgService>();
    })
    .Build();

await host.RunAsync();

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DepositMonitoring;

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
        services.AddHostedService<PollingMonitoringBgService>();
    })
    .Build();

await host.RunAsync();

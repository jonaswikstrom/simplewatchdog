using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleWatchDog;

public class HostService : IHostedService
{
    private readonly ILogger<HostService> logger;
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly IOptions<WatchDogSettings> settings;
    private readonly IServiceProvider sp;

    public HostService(ILogger<HostService> logger, IHostApplicationLifetime applicationLifetime, IOptions<WatchDogSettings> settings, IServiceProvider sp)
    {
        this.logger = logger;
        this.applicationLifetime = applicationLifetime;
        this.settings = settings;
        this.sp = sp;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"Startup delayed by {settings.Value.StartupDelay.TotalMilliseconds} milliseconds");
        await Task.Delay(settings.Value.StartupDelay, cancellationToken);
        logger.LogInformation("Initiating watch dogs");

        await using var scope = sp.CreateAsyncScope();
        foreach (var watchDogSetting in settings.Value.WatchDogs)
        {
            scope.ServiceProvider.GetRequiredService<WatchDog>().Init(watchDogSetting);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
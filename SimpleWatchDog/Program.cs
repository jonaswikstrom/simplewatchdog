using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleWatchDog;

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, true)
            .AddEnvironmentVariables()
            .Build();


        var hostBuilder = new HostBuilder().ConfigureServices(p =>
        {
            p.AddLogging(loggingBuilder => loggingBuilder.AddConsole())
                .Configure<WatchDogSettings>(configuration.GetSection("Settings"))
                .AddTransient<WatchDog>()
                .AddHostedService<HostService>();
        });

        await hostBuilder.RunConsoleAsync(cts.Token);
    }
}

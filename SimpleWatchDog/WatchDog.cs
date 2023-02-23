using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Timer = System.Timers.Timer;

namespace SimpleWatchDog;

public class WatchDog : IDisposable
{
    private readonly ILogger<WatchDog> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly Timer timer;
    private WatchDogSetting settings = null!;
    private Ping ping = null!;
    private IPAddress ipAddress = null!;
    private List<DateTime> invalidPings = new();

    public WatchDog(ILogger<WatchDog> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.logger = logger;
        this.hostApplicationLifetime = hostApplicationLifetime;
        timer = new Timer();
        timer.Elapsed += TimerOnElapsed;

    }

#pragma warning disable CS1998
    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        timer.Stop();
        var pingReply = await ping.SendPingAsync(ipAddress, (int)settings!.Ping.Timeout.TotalMilliseconds);

        if (pingReply.Status == IPStatus.Success && pingReply.RoundtripTime <= settings!.Ping.Timeout.TotalMilliseconds)
        {
            logger.LogInformation($"{settings.Name}: Ping reply from {ipAddress} successful in {pingReply.RoundtripTime} milliseconds");
            timer.Start();
            return;
        }

        var logWarning = pingReply.Status != IPStatus.Success ? $"invalid status code {pingReply.Status}" : $"to high roundtrip time {pingReply.RoundtripTime}";
        logger.LogWarning($"{settings.Name}: Ping reply to {ipAddress} failed due to {logWarning}");

        invalidPings.Add(DateTime.Now);
        invalidPings = invalidPings.Where(t => DateTime.Now - t <= settings.Treshold.SlidingWindow).ToList();

        if (invalidPings.Count < settings.Treshold.FailureCount)
        {
            logger.LogInformation(
                $"{settings.Name}: Failure count is {invalidPings.Count} of max {settings.Treshold.FailureCount} during timespan {settings.Treshold.SlidingWindow}");
            timer.Start();
            return;
        }

        logger.LogWarning($"{settings.Name}: Failure count is {settings.Treshold.FailureCount} during timespan {settings.Treshold.SlidingWindow}. Now executing SSH command");

        using var client = new SshClient(settings.SshCommand.Ip, settings.SshCommand.UserName, settings.SshCommand.Password);
        client.Connect();
        var commandResult = client.RunCommand(settings.SshCommand.Command);

        if (commandResult.ExitStatus >= 0)
        {
            logger.LogInformation($"{settings.Name}: Command executed successfully: {commandResult.Result}");
        }
        else
        {
            logger.LogWarning($"{settings.Name}: Command executed with error: {commandResult.Error}");
        }

        client.Disconnect();
        logger.LogInformation($"{settings.Name}: Command delay of {settings.SshCommand.CommandDelay}");
        await Task.Delay(settings.SshCommand.CommandDelay);
        invalidPings = new List<DateTime>();

        timer.Start();
    }


    // ReSharper disable once ParameterHidesMember
    public void Init(WatchDogSetting settings)
    {
        this.settings = settings;

        ipAddress = IPAddress.Parse(settings.Ping.Ip);
        ping = new Ping();
        
        timer.Interval = settings.Treshold.Interval.TotalMilliseconds;
        timer.Start();

        logger.LogInformation($"{settings.Name}: Initiated with delay of {timer.Interval} milliseconds ");
    }

    public void Dispose()
    {
        timer.Dispose();
        ping.Dispose();
    }
}
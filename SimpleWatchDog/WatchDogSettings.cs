namespace SimpleWatchDog;

public class WatchDogSettings
{
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(1);
    public WatchDogSetting[] WatchDogs { get; set; } = null!;
}

public class Test
{
    public TimeSpan Name { get; set; }
}

public class WatchDogSetting
{
    public string Name { get; set; } = null!;
    public PingSettings Ping { get; set; } = null!;

    public Treshold Treshold { get; set; } = null!;
    public SshCommand SshCommand { get; set; } = null!;
}

public class PingSettings
{
    public string Ip { get; set; } = null!;
    public TimeSpan Timeout { get; set; }
}

public class Treshold
{
    public TimeSpan Interval { get; set; }
    public TimeSpan SlidingWindow { get; set; }
    public int FailureCount { get; set; }
}

public class SshCommand
{
    public string Ip { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Command { get; set; } = null!;
    public TimeSpan CommandDelay { get; set; }
}
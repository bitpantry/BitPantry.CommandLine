namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public record CommandLineClientSettings(TimeSpan TokenRefreshMonitorInterval, TimeSpan TokenRefreshThreshold) { }
}

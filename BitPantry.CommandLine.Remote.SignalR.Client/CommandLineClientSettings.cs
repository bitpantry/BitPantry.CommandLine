namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// CommandLine client settings
    /// </summary>
    /// <param name="TokenRefreshMonitorInterval">The interval used by the <see cref="AccessTokenManager"/> to see whether or not the access token should be refreshed</param>
    /// <param name="TokenRefreshThreshold">The amount of time before the token expires when the <see cref="AccessTokenManager"/> will attempt to refresh the access token</param>
    public record CommandLineClientSettings(TimeSpan TokenRefreshMonitorInterval, TimeSpan TokenRefreshThreshold) { }
}

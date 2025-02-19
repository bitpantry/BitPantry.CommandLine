namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestEnvironmentOptions
    {
        public bool UseAuthentication { get; set; } = true;
        public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.Zero;
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
    }
}

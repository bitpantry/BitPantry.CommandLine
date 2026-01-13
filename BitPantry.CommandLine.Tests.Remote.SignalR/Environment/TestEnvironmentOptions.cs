#nullable enable

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestEnvironmentOptions
    {
        public bool UseAuthentication { get; set; } = true;
        public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Unique identifier for this test run. Auto-generated if not set.
        /// Used for creating isolated test directories.
        /// </summary>
        public string TestId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The storage root path for file transfers. If not set, a temp directory will be used.
        /// </summary>
        public string ServerStorageRoot { get; set; } = "./cli-storage";

        /// <summary>
        /// Maximum file size in bytes for file transfers. Default is 100MB.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for file transfers. Null means all extensions are allowed.
        /// </summary>
        public string[]? AllowedExtensions { get; set; }
    }
}

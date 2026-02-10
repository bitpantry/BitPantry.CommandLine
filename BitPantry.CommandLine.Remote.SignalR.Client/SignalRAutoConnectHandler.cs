using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Handles automatic server connection for single-command execution using the profile system.
    /// Profile resolution order: RequestedProfileName → BITPANTRY_PROFILE env var → default profile.
    /// 
    /// This handler does NOT take IServerProxy in its constructor to avoid circular dependency.
    /// The proxy is passed as a method parameter to EnsureConnectedAsync.
    /// </summary>
    public class SignalRAutoConnectHandler : IAutoConnectHandler
    {
        /// <summary>
        /// The environment variable name used to specify a profile for auto-connect.
        /// </summary>
        public const string ProfileEnvironmentVariable = "BITPANTRY_PROFILE";

        private readonly ILogger<SignalRAutoConnectHandler> _logger;
        private readonly IProfileManager _profileManager;
        private readonly IProfileConnectionState _profileConnectionState;
        private readonly ConnectionService _connectionService;

        /// <inheritdoc />
        public string RequestedProfileName { get; set; }

        /// <inheritdoc />
        public bool AutoConnectEnabled { get; set; }

        /// <summary>
        /// When set, this value is used instead of reading the <see cref="ProfileEnvironmentVariable"/>
        /// environment variable during profile resolution. This allows tests to avoid
        /// process-wide environment variable manipulation.
        /// </summary>
        public string EnvironmentProfileOverride { get; set; }

        public SignalRAutoConnectHandler(
            ILogger<SignalRAutoConnectHandler> logger,
            IProfileManager profileManager,
            IProfileConnectionState profileConnectionState,
            ConnectionService connectionService)
        {
            _logger = logger;
            _profileManager = profileManager;
            _profileConnectionState = profileConnectionState;
            _connectionService = connectionService;
        }

        /// <inheritdoc />
        public async Task<bool> EnsureConnectedAsync(IServerProxy proxy, CancellationToken token = default)
        {
            if (!AutoConnectEnabled)
                return proxy.ConnectionState == ServerProxyConnectionState.Connected;

            // Resolve the target profile — throws if an explicitly requested profile is not found
            var profile = await ResolveProfile(token);

            // Already connected — verify we're connected to the right profile
            if (proxy.ConnectionState == ServerProxyConnectionState.Connected)
            {
                if (profile != null
                    && !string.IsNullOrEmpty(_profileConnectionState.ConnectedProfileName)
                    && !_profileConnectionState.ConnectedProfileName.Equals(profile.Name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Connected via profile '{_profileConnectionState.ConnectedProfileName}' but profile '{profile.Name}' was requested — disconnect first");
                }

                return true;
            }

            // Not connected, no profile available — can't auto-connect
            if (profile == null)
            {
                _logger.LogDebug("No profile resolved for auto-connect — skipping");
                return false;
            }

            // Attempt auto-connect
            _logger.LogDebug("Auto-connecting using profile '{ProfileName}' to {Uri}", profile.Name, profile.Uri);

            try
            {
                await _connectionService.ConnectWithAuthAsync(proxy, profile.Uri, profile.ApiKey, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-connect failed for profile '{ProfileName}'", profile.Name);
                return false;
            }

            // Track profile connection state
            _profileConnectionState.ConnectedProfileName = profile.Name;

            _logger.LogInformation("Auto-connected to {Uri} using profile '{ProfileName}'", profile.Uri, profile.Name);
            return true;
        }

        /// <summary>
        /// Resolves the profile to use for auto-connect.
        /// Resolution order: RequestedProfileName → BITPANTRY_PROFILE env var → default profile.
        /// Throws if an explicitly specified profile (via --profile or env var) does not exist.
        /// Returns null only when no profile is configured at all.
        /// </summary>
        private async Task<ServerProfile> ResolveProfile(CancellationToken token)
        {
            // 1. Explicitly requested profile name (from --profile global arg)
            if (!string.IsNullOrEmpty(RequestedProfileName))
            {
                var profile = await _profileManager.GetProfileAsync(RequestedProfileName, token);
                if (profile == null)
                    throw new InvalidOperationException($"Profile '{RequestedProfileName}' not found");
                return profile;
            }

            // 2. Environment variable (or test override)
            var envProfile = EnvironmentProfileOverride ?? Environment.GetEnvironmentVariable(ProfileEnvironmentVariable);
            if (!string.IsNullOrEmpty(envProfile))
            {
                _logger.LogDebug("Using profile from {EnvVar}: '{ProfileName}'", ProfileEnvironmentVariable, envProfile);
                var profile = await _profileManager.GetProfileAsync(envProfile, token);
                if (profile == null)
                    throw new InvalidOperationException(
                        $"Profile '{envProfile}' specified in environment variable {ProfileEnvironmentVariable} not found");
                return profile;
            }

            // 3. Default profile
            var defaultName = await _profileManager.GetDefaultProfileNameAsync(token);
            if (!string.IsNullOrEmpty(defaultName))
            {
                _logger.LogDebug("Using default profile: '{ProfileName}'", defaultName);
                return await _profileManager.GetProfileAsync(defaultName, token);
            }

            return null;
        }
    }
}
